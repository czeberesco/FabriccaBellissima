using System;
using System.Collections.Generic;
using System.Linq;

namespace FabriccaBellissima.Factory
{
    public sealed class FactorySimulation : IFactorySimulation
    {
        private readonly FactoryScenario _scenario;
        private readonly List<NodeState> _nodes = new List<NodeState>();
        private readonly Dictionary<int, NodeState> _nodesById = new Dictionary<int, NodeState>();
        private readonly Dictionary<PortKey, FactoryConnection> _connections = new Dictionary<PortKey, FactoryConnection>();
        private readonly List<FactoryEvent> _events = new List<FactoryEvent>();
        private FactoryCounters _counters = new FactoryCounters();
        private long _nextItemId;

        public FactorySimulation(FactoryScenario scenario)
        {
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            ValidateScenario(_scenario);
            BuildInitialState();
        }

        public long Tick { get; private set; }
        public float TickDurationSeconds => _scenario.tickDurationMicroseconds / 1000000f;
        public FactorySnapshot CurrentSnapshot => CreateSnapshot();
        public IReadOnlyList<FactoryNodeConfig> NodeConfigs => _scenario.nodes;
        public IReadOnlyList<FactoryConnection> Connections => _scenario.connections;
        public IReadOnlyList<FactoryEvent> LastEvents => _events;

        public static FactorySimulation FromJson(string json) => new FactorySimulation(FactoryScenario.FromJson(json));

        public void StepOneTick()
        {
            Tick++;
            _events.Clear();

            foreach (NodeState node in _nodes)
            {
                LocalUpdate(node);
            }

            var proposals = new List<TransferProposal>();
            foreach (NodeState node in _nodes)
            {
                if (TryCreateProposal(node, out TransferProposal proposal))
                {
                    proposals.Add(proposal);
                }
            }

            proposals.Sort((left, right) =>
            {
                int nodeComparison = left.Source.Config.nodeId.CompareTo(right.Source.Config.nodeId);
                return nodeComparison != 0 ? nodeComparison : left.SourcePortId.CompareTo(right.SourcePortId);
            });

            foreach (TransferProposal proposal in proposals)
            {
                FactoryConnection connection = proposal.Connection;
                NodeState destination = _nodesById[connection.destinationNodeId];
                if (!CanAccept(destination, connection.destinationPortId, proposal.Item))
                {
                    continue;
                }

                FactoryItem transferred = RemoveProposedOutput(proposal.Source, proposal.Item.id);
                AddEvent(FactoryEventKind.Transferred, proposal.Source.Config.nodeId, proposal.SourcePortId,
                    transferred, transferred.resource, transferred.resource, connection.destinationNodeId,
                    connection.destinationPortId);
                Accept(destination, connection.destinationPortId, transferred);
            }
        }

        public void StepTicks(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Tick count cannot be negative.");
            }

            for (int index = 0; index < count; index++)
            {
                StepOneTick();
            }
        }

        public void ResetSimulation()
        {
            BuildInitialState();
        }

        public void SetNodeEnabled(int nodeId, bool enabled)
        {
            if (!_nodesById.TryGetValue(nodeId, out NodeState node))
            {
                throw new ArgumentException($"Unknown node ID {nodeId}.", nameof(nodeId));
            }

            node.Enabled = enabled;
        }

        public bool GetNodeEnabled(int nodeId) => _nodesById[nodeId].Enabled;

        public string ExportCanonicalSnapshot(bool pretty = false) => CreateSnapshot().ToCanonicalJson(pretty);

        private void BuildInitialState()
        {
            Tick = _scenario.initialTick;
            _nextItemId = _scenario.nextItemId;
            _counters = new FactoryCounters();
            _events.Clear();
            _nodes.Clear();
            _nodesById.Clear();
            _connections.Clear();

            foreach (FactoryNodeConfig config in _scenario.nodes.OrderBy(node => node.nodeId))
            {
                var node = new NodeState(config);
                _nodes.Add(node);
                _nodesById.Add(config.nodeId, node);
            }

            foreach (FactoryConnection connection in _scenario.connections)
            {
                _connections.Add(new PortKey(connection.sourceNodeId, connection.sourcePortId), connection);
            }

            foreach (FactoryNodeInitialState initial in _scenario.initialStates)
            {
                NodeState node = _nodesById[initial.nodeId];
                node.WorkProgress = initial.workProgress;
                node.FuelRemaining = initial.fuelRemaining;
                node.AppliedCharges = initial.appliedCharges;
                node.SprayProgress = initial.sprayProgress;
                node.ReservoirCharges = initial.reservoirCharges;
                node.ReservoirColorId = initial.reservoirColorId ?? string.Empty;
                node.OutputItem = CloneOrNull(initial.outputItem);
                node.WorkItem = CloneOrNull(initial.workItem);
                node.WaitingCoal = CloneOrNull(initial.waitingCoal);
                node.WaitingCan = CloneOrNull(initial.waitingCan);
                node.BeltItems.Clear();
                foreach (FactoryBeltItem item in initial.beltItems)
                {
                    node.BeltItems.Add(item.Clone());
                }
            }
        }

        private void LocalUpdate(NodeState node)
        {
            if (!node.Enabled)
            {
                return;
            }

            switch (node.Config.kind)
            {
                case FactoryNodeKind.Extractor:
                    UpdateExtractor(node);
                    break;
                case FactoryNodeKind.Conveyor:
                    UpdateConveyor(node);
                    break;
                case FactoryNodeKind.Furnace:
                    UpdateFurnace(node);
                    break;
                case FactoryNodeKind.ColoringStation:
                    UpdateColoringStation(node);
                    break;
            }
        }

        private void UpdateExtractor(NodeState node)
        {
            if (node.OutputItem != null)
            {
                return;
            }

            node.WorkProgress++;
            if (node.WorkProgress < node.Config.durationTicks)
            {
                return;
            }

            node.WorkProgress = 0;
            node.OutputItem = new FactoryItem
            {
                id = _nextItemId++,
                resource = node.Config.resource,
                colorId = node.Config.resource == FactoryResourceType.PaintCan ? node.Config.colorId : string.Empty,
                charges = node.Config.resource == FactoryResourceType.PaintCan ? node.Config.paintCanCharges : 0
            };
            IncrementGenerated(node.Config.resource);
            AddEvent(FactoryEventKind.Generated, node.Config.nodeId, FactoryPorts.Output, node.OutputItem,
                FactoryResourceType.None, node.OutputItem.resource);
        }

        private static void UpdateConveyor(NodeState node)
        {
            if (node.BeltItems.Count == 0)
            {
                return;
            }

            FactoryBeltItem front = node.BeltItems[0];
            front.progressUnits = Math.Min(node.Config.lengthUnits,
                front.progressUnits + node.Config.movementPerTick);

            for (int index = 1; index < node.BeltItems.Count; index++)
            {
                FactoryBeltItem current = node.BeltItems[index];
                int maximum = Math.Max(current.progressUnits,
                    node.BeltItems[index - 1].progressUnits - node.Config.spacingUnits);
                current.progressUnits = Math.Min(maximum, current.progressUnits + node.Config.movementPerTick);
            }
        }

        private void UpdateFurnace(NodeState node)
        {
            if (node.FuelRemaining == 0 && node.WaitingCoal != null)
            {
                FactoryItem coal = node.WaitingCoal;
                node.WaitingCoal = null;
                node.FuelRemaining = node.Config.burnTicks;
                _counters.coalConsumed++;
                AddEvent(FactoryEventKind.CoalIgnited, node.Config.nodeId, FactoryPorts.FurnaceFuel, coal,
                    FactoryResourceType.Coal, FactoryResourceType.None, amount: node.Config.burnTicks);
            }

            if (node.FuelRemaining <= 0)
            {
                return;
            }

            if (node.WorkItem != null && node.WorkItem.resource == FactoryResourceType.IronOre)
            {
                node.WorkProgress++;
                if (node.WorkProgress >= node.Config.smeltTicks)
                {
                    node.WorkItem.resource = FactoryResourceType.IronBar;
                    node.WorkProgress = node.Config.smeltTicks;
                    _counters.ironBarsMade++;
                    AddEvent(FactoryEventKind.Transformed, node.Config.nodeId, FactoryPorts.FurnaceOutput,
                        node.WorkItem, FactoryResourceType.IronOre, FactoryResourceType.IronBar);
                }
            }

            node.FuelRemaining--;
        }

        private void UpdateColoringStation(NodeState node)
        {
            if (node.ReservoirCharges == 0 && node.WaitingCan != null)
            {
                FactoryItem can = node.WaitingCan;
                node.WaitingCan = null;
                node.ReservoirCharges = can.charges;
                node.ReservoirColorId = can.colorId;
                _counters.cansOpened++;
                AddEvent(FactoryEventKind.CanOpened, node.Config.nodeId, FactoryPorts.ColoringCan, can,
                    FactoryResourceType.PaintCan, FactoryResourceType.None, amount: can.charges);
            }

            if (node.WorkItem == null || node.WorkItem.resource != FactoryResourceType.IronBar ||
                node.ReservoirCharges <= 0)
            {
                return;
            }

            node.SprayProgress++;
            if (node.SprayProgress < node.Config.sprayTicks)
            {
                return;
            }

            node.SprayProgress = 0;
            node.ReservoirCharges--;
            node.AppliedCharges++;
            _counters.chargesApplied++;
            AddEvent(FactoryEventKind.ChargeApplied, node.Config.nodeId, FactoryPorts.ColoringBar,
                node.WorkItem, FactoryResourceType.IronBar, FactoryResourceType.IronBar,
                amount: node.AppliedCharges);

            if (node.AppliedCharges < node.Config.chargesPerBar)
            {
                return;
            }

            node.WorkItem.resource = FactoryResourceType.PaintedIronBar;
            node.WorkItem.colorId = node.Config.colorId;
            _counters.paintedBarsMade++;
            AddEvent(FactoryEventKind.Transformed, node.Config.nodeId, FactoryPorts.ColoringOutput,
                node.WorkItem, FactoryResourceType.IronBar, FactoryResourceType.PaintedIronBar);
        }

        private bool TryCreateProposal(NodeState source, out TransferProposal proposal)
        {
            proposal = default;
            if (!source.Enabled)
            {
                return false;
            }

            int portId;
            FactoryItem item;
            switch (source.Config.kind)
            {
                case FactoryNodeKind.Extractor:
                    portId = FactoryPorts.Output;
                    item = source.OutputItem;
                    break;
                case FactoryNodeKind.Conveyor:
                    portId = FactoryPorts.Output;
                    item = source.BeltItems.Count > 0 &&
                           source.BeltItems[0].progressUnits >= source.Config.lengthUnits
                        ? source.BeltItems[0].item
                        : null;
                    break;
                case FactoryNodeKind.Furnace:
                    portId = FactoryPorts.FurnaceOutput;
                    item = source.WorkItem != null && source.WorkItem.resource == FactoryResourceType.IronBar
                        ? source.WorkItem
                        : null;
                    break;
                case FactoryNodeKind.ColoringStation:
                    portId = FactoryPorts.ColoringOutput;
                    item = source.WorkItem != null &&
                           source.WorkItem.resource == FactoryResourceType.PaintedIronBar
                        ? source.WorkItem
                        : null;
                    break;
                default:
                    return false;
            }

            if (item == null || !_connections.TryGetValue(new PortKey(source.Config.nodeId, portId),
                    out FactoryConnection connection))
            {
                return false;
            }

            proposal = new TransferProposal(source, portId, item, connection);
            return true;
        }

        private static bool CanAccept(NodeState destination, int portId, FactoryItem item)
        {
            if (!destination.Enabled || item == null)
            {
                return false;
            }

            switch (destination.Config.kind)
            {
                case FactoryNodeKind.Conveyor:
                    if (portId != FactoryPorts.Input || destination.BeltItems.Count >= destination.Config.capacity)
                    {
                        return false;
                    }

                    return destination.BeltItems.Count == 0 ||
                           destination.BeltItems[destination.BeltItems.Count - 1].progressUnits >=
                           destination.Config.spacingUnits;
                case FactoryNodeKind.Furnace:
                    if (portId == FactoryPorts.FurnaceOre)
                    {
                        return item.resource == FactoryResourceType.IronOre && destination.WorkItem == null;
                    }

                    return portId == FactoryPorts.FurnaceFuel && item.resource == FactoryResourceType.Coal &&
                           destination.WaitingCoal == null;
                case FactoryNodeKind.ColoringStation:
                    if (portId == FactoryPorts.ColoringBar)
                    {
                        return item.resource == FactoryResourceType.IronBar && destination.WorkItem == null;
                    }

                    return portId == FactoryPorts.ColoringCan && item.resource == FactoryResourceType.PaintCan &&
                           destination.WaitingCan == null && item.charges > 0 &&
                           string.Equals(item.colorId, destination.Config.colorId, StringComparison.Ordinal);
                case FactoryNodeKind.Sink:
                    return portId == FactoryPorts.Input && item.resource == FactoryResourceType.PaintedIronBar &&
                           string.Equals(item.colorId, destination.Config.colorId, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private void Accept(NodeState destination, int portId, FactoryItem item)
        {
            switch (destination.Config.kind)
            {
                case FactoryNodeKind.Conveyor:
                    destination.BeltItems.Add(new FactoryBeltItem { item = item, progressUnits = 0 });
                    break;
                case FactoryNodeKind.Furnace when portId == FactoryPorts.FurnaceOre:
                    destination.WorkItem = item;
                    destination.WorkProgress = 0;
                    break;
                case FactoryNodeKind.Furnace:
                    destination.WaitingCoal = item;
                    break;
                case FactoryNodeKind.ColoringStation when portId == FactoryPorts.ColoringBar:
                    destination.WorkItem = item;
                    destination.AppliedCharges = 0;
                    destination.SprayProgress = 0;
                    break;
                case FactoryNodeKind.ColoringStation:
                    destination.WaitingCan = item;
                    break;
                case FactoryNodeKind.Sink:
                    destination.ConsumedItemIds.Add(item.id);
                    if (!destination.SinkByColor.ContainsKey(item.colorId))
                    {
                        destination.SinkByColor.Add(item.colorId, 0);
                    }

                    destination.SinkByColor[item.colorId]++;
                    _counters.sinkConsumed++;
                    AddEvent(FactoryEventKind.SinkConsumed, destination.Config.nodeId, FactoryPorts.Input, item,
                        FactoryResourceType.PaintedIronBar, FactoryResourceType.None);
                    break;
                default:
                    throw new InvalidOperationException("Destination accepted through an unsupported port.");
            }
        }

        private static FactoryItem RemoveProposedOutput(NodeState source, long expectedItemId)
        {
            FactoryItem result;
            switch (source.Config.kind)
            {
                case FactoryNodeKind.Extractor:
                    result = source.OutputItem;
                    source.OutputItem = null;
                    break;
                case FactoryNodeKind.Conveyor:
                    result = source.BeltItems[0].item;
                    source.BeltItems.RemoveAt(0);
                    break;
                case FactoryNodeKind.Furnace:
                    result = source.WorkItem;
                    source.WorkItem = null;
                    source.WorkProgress = 0;
                    break;
                case FactoryNodeKind.ColoringStation:
                    result = source.WorkItem;
                    source.WorkItem = null;
                    source.WorkProgress = 0;
                    source.AppliedCharges = 0;
                    source.SprayProgress = 0;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported output source.");
            }

            if (result == null || result.id != expectedItemId)
            {
                throw new InvalidOperationException("Proposed transfer item ownership changed before commit.");
            }

            return result;
        }

        private FactorySnapshot CreateSnapshot()
        {
            var snapshot = new FactorySnapshot
            {
                tick = Tick,
                tickDurationMicroseconds = _scenario.tickDurationMicroseconds,
                nextItemId = _nextItemId,
                counters = _counters.Clone()
            };

            foreach (NodeState node in _nodes)
            {
                var nodeSnapshot = new FactoryNodeSnapshot
                {
                    nodeId = node.Config.nodeId,
                    displayName = node.Config.displayName,
                    kind = node.Config.kind,
                    enabled = node.Enabled,
                    status = GetStatus(node),
                    configuredResource = node.Config.resource,
                    configuredColorId = node.Config.colorId,
                    durationTicks = node.Config.durationTicks,
                    paintCanCharges = node.Config.paintCanCharges,
                    workProgress = node.WorkProgress,
                    workRequired = GetWorkRequired(node),
                    fuelRemaining = node.FuelRemaining,
                    burnTicks = node.Config.burnTicks,
                    reservoirCharges = node.ReservoirCharges,
                    reservoirColorId = node.ReservoirColorId,
                    appliedCharges = node.AppliedCharges,
                    chargesPerBar = node.Config.chargesPerBar,
                    sprayProgress = node.SprayProgress,
                    sprayTicks = node.Config.sprayTicks,
                    lengthUnits = node.Config.lengthUnits,
                    movementPerTick = node.Config.movementPerTick,
                    capacity = node.Config.capacity,
                    spacingUnits = node.Config.spacingUnits,
                    outputItem = CloneOrNull(node.OutputItem),
                    workItem = CloneOrNull(node.WorkItem),
                    waitingCoal = CloneOrNull(node.WaitingCoal),
                    waitingCan = CloneOrNull(node.WaitingCan),
                    sinkTotal = node.ConsumedItemIds.Count
                };

                foreach (FactoryBeltItem beltItem in node.BeltItems)
                {
                    nodeSnapshot.beltItems.Add(beltItem.Clone());
                }

                foreach (KeyValuePair<string, int> entry in node.SinkByColor)
                {
                    nodeSnapshot.sinkColors.Add(entry.Key);
                    nodeSnapshot.sinkColorCounts.Add(entry.Value);
                }

                nodeSnapshot.consumedItemIds.AddRange(node.ConsumedItemIds);
                snapshot.nodes.Add(nodeSnapshot);
            }

            foreach (FactoryConnection connection in _scenario.connections
                         .OrderBy(value => value.sourceNodeId)
                         .ThenBy(value => value.sourcePortId)
                         .ThenBy(value => value.destinationNodeId)
                         .ThenBy(value => value.destinationPortId))
            {
                snapshot.connections.Add(new FactoryConnection
                {
                    sourceNodeId = connection.sourceNodeId,
                    sourcePortId = connection.sourcePortId,
                    destinationNodeId = connection.destinationNodeId,
                    destinationPortId = connection.destinationPortId
                });
            }

            foreach (FactoryEvent factoryEvent in _events)
            {
                snapshot.events.Add(factoryEvent);
            }

            return snapshot;
        }

        private static int GetWorkRequired(NodeState node)
        {
            switch (node.Config.kind)
            {
                case FactoryNodeKind.Extractor:
                    return node.Config.durationTicks;
                case FactoryNodeKind.Furnace:
                    return node.Config.smeltTicks;
                case FactoryNodeKind.ColoringStation:
                    return node.Config.sprayTicks;
                default:
                    return 0;
            }
        }

        private static string GetStatus(NodeState node)
        {
            if (!node.Enabled)
            {
                return "Disabled";
            }

            switch (node.Config.kind)
            {
                case FactoryNodeKind.Extractor:
                    return node.OutputItem != null ? "Output blocked" : "Extracting";
                case FactoryNodeKind.Conveyor:
                    if (node.BeltItems.Count == 0) return "Waiting for input";
                    if (node.BeltItems[0].progressUnits >= node.Config.lengthUnits) return "Exit blocked";
                    return $"Moving {node.BeltItems.Count}/{node.Config.capacity}";
                case FactoryNodeKind.Furnace:
                    if (node.WorkItem != null && node.WorkItem.resource == FactoryResourceType.IronBar)
                        return "Output blocked";
                    if (node.WorkItem == null) return node.FuelRemaining > 0 ? "Idle (fuel burning)" : "Waiting for ore";
                    if (node.FuelRemaining == 0 && node.WaitingCoal == null) return "Waiting for fuel";
                    return "Smelting";
                case FactoryNodeKind.ColoringStation:
                    if (node.WorkItem != null && node.WorkItem.resource == FactoryResourceType.PaintedIronBar)
                        return "Output blocked";
                    if (node.WorkItem == null) return "Waiting for bar";
                    if (node.ReservoirCharges == 0 && node.WaitingCan == null) return "Waiting for paint";
                    return "Spraying";
                case FactoryNodeKind.Sink:
                    return $"Consumed {node.ConsumedItemIds.Count}";
                default:
                    return string.Empty;
            }
        }

        private void IncrementGenerated(FactoryResourceType resource)
        {
            switch (resource)
            {
                case FactoryResourceType.IronOre:
                    _counters.ironOreGenerated++;
                    break;
                case FactoryResourceType.Coal:
                    _counters.coalGenerated++;
                    break;
                case FactoryResourceType.PaintCan:
                    _counters.paintCansGenerated++;
                    break;
            }
        }

        private void AddEvent(FactoryEventKind kind, int nodeId, int portId, FactoryItem item,
            FactoryResourceType before, FactoryResourceType after, int otherNodeId = 0, int otherPortId = 0,
            int amount = 0)
        {
            _events.Add(new FactoryEvent
            {
                tick = Tick,
                kind = kind,
                nodeId = nodeId,
                portId = portId,
                otherNodeId = otherNodeId,
                otherPortId = otherPortId,
                itemId = item?.id ?? 0,
                resourceBefore = before,
                resourceAfter = after,
                colorId = item?.colorId ?? string.Empty,
                amount = amount
            });
        }

        private static FactoryItem CloneOrNull(FactoryItem item) => item?.Clone();

        private static void ValidateScenario(FactoryScenario scenario)
        {
            if (scenario.formatVersion != 1)
                throw new ArgumentException($"Unsupported scenario version {scenario.formatVersion}.");
            if (scenario.tickDurationMicroseconds <= 0)
                throw new ArgumentException("Tick duration in microseconds must be positive.");
            if (scenario.nextItemId <= 0) throw new ArgumentException("Next item ID must be positive.");
            if (scenario.nodes == null || scenario.nodes.Count == 0) throw new ArgumentException("Scenario needs nodes.");

            var ids = new HashSet<int>();
            foreach (FactoryNodeConfig node in scenario.nodes)
            {
                if (node == null || node.nodeId <= 0 || !ids.Add(node.nodeId))
                    throw new ArgumentException("Node IDs must be positive and unique.");
                ValidateNode(node);
            }

            var sources = new HashSet<PortKey>();
            var destinations = new HashSet<PortKey>();
            foreach (FactoryConnection connection in scenario.connections)
            {
                if (!ids.Contains(connection.sourceNodeId) || !ids.Contains(connection.destinationNodeId))
                    throw new ArgumentException("Every connection must reference existing nodes.");
                if (connection.sourceNodeId == connection.destinationNodeId)
                    throw new ArgumentException("Self connections are not supported.");
                if (!sources.Add(new PortKey(connection.sourceNodeId, connection.sourcePortId)))
                    throw new ArgumentException("An output port can have only one connection.");
                if (!destinations.Add(new PortKey(connection.destinationNodeId, connection.destinationPortId)))
                    throw new ArgumentException("An input port can have only one connection.");
                FactoryNodeConfig source = scenario.nodes.First(node => node.nodeId == connection.sourceNodeId);
                FactoryNodeConfig destination = scenario.nodes.First(node => node.nodeId == connection.destinationNodeId);
                if (!IsOutputPort(source.kind, connection.sourcePortId))
                    throw new ArgumentException($"Node {source.nodeId} has no output port {connection.sourcePortId}.");
                if (!IsInputPort(destination.kind, connection.destinationPortId))
                    throw new ArgumentException($"Node {destination.nodeId} has no input port {connection.destinationPortId}.");
            }

            ValidateAcyclic(scenario);
            ValidateInitialState(scenario, ids);
        }

        private static void ValidateNode(FactoryNodeConfig node)
        {
            switch (node.kind)
            {
                case FactoryNodeKind.Extractor:
                    if (node.durationTicks <= 0) throw new ArgumentException($"Extractor {node.nodeId} duration must be positive.");
                    if (node.resource != FactoryResourceType.IronOre && node.resource != FactoryResourceType.Coal &&
                        node.resource != FactoryResourceType.PaintCan)
                        throw new ArgumentException($"Extractor {node.nodeId} has an unsupported resource.");
                    if (node.resource == FactoryResourceType.PaintCan && (node.paintCanCharges <= 0 || string.IsNullOrEmpty(node.colorId)))
                        throw new ArgumentException($"Paint extractor {node.nodeId} needs charges and a color.");
                    break;
                case FactoryNodeKind.Conveyor:
                    if (node.capacity <= 0 || node.lengthUnits < node.capacity || node.movementPerTick <= 0 ||
                        node.spacingUnits != Math.Max(1, node.lengthUnits / node.capacity))
                        throw new ArgumentException($"Conveyor {node.nodeId} has invalid length, speed, capacity, or spacing.");
                    break;
                case FactoryNodeKind.Furnace:
                    if (node.burnTicks <= 0 || node.smeltTicks <= 0)
                        throw new ArgumentException($"Furnace {node.nodeId} durations must be positive.");
                    break;
                case FactoryNodeKind.ColoringStation:
                    if (node.sprayTicks <= 0 || node.chargesPerBar <= 0 || string.IsNullOrEmpty(node.colorId))
                        throw new ArgumentException($"Coloring station {node.nodeId} configuration is invalid.");
                    break;
            }
        }

        private static bool IsOutputPort(FactoryNodeKind kind, int portId)
        {
            switch (kind)
            {
                case FactoryNodeKind.Extractor:
                case FactoryNodeKind.Conveyor:
                    return portId == FactoryPorts.Output;
                case FactoryNodeKind.Furnace:
                    return portId == FactoryPorts.FurnaceOutput;
                case FactoryNodeKind.ColoringStation:
                    return portId == FactoryPorts.ColoringOutput;
                default:
                    return false;
            }
        }

        private static bool IsInputPort(FactoryNodeKind kind, int portId)
        {
            switch (kind)
            {
                case FactoryNodeKind.Conveyor:
                case FactoryNodeKind.Sink:
                    return portId == FactoryPorts.Input;
                case FactoryNodeKind.Furnace:
                    return portId == FactoryPorts.FurnaceOre || portId == FactoryPorts.FurnaceFuel;
                case FactoryNodeKind.ColoringStation:
                    return portId == FactoryPorts.ColoringBar || portId == FactoryPorts.ColoringCan;
                default:
                    return false;
            }
        }

        private static void ValidateInitialState(FactoryScenario scenario, HashSet<int> nodeIds)
        {
            var stateNodes = new HashSet<int>();
            var itemIds = new HashSet<long>();
            long maximumId = 0;
            foreach (FactoryNodeInitialState state in scenario.initialStates)
            {
                if (state == null || !nodeIds.Contains(state.nodeId) || !stateNodes.Add(state.nodeId))
                    throw new ArgumentException("Initial states must reference unique existing nodes.");
                foreach (FactoryItem item in EnumerateItems(state))
                {
                    if (item.id <= 0 || !itemIds.Add(item.id))
                        throw new ArgumentException("Initial item IDs must be positive and globally unique.");
                    maximumId = Math.Max(maximumId, item.id);
                }

                FactoryNodeConfig config = scenario.nodes.First(node => node.nodeId == state.nodeId);
                if (state.beltItems.Count > config.capacity && config.kind == FactoryNodeKind.Conveyor)
                    throw new ArgumentException($"Initial belt {state.nodeId} exceeds capacity.");
                if (state.beltItems.Any(item => item.progressUnits < 0 || item.progressUnits > config.lengthUnits))
                    throw new ArgumentException($"Initial belt {state.nodeId} progress is out of range.");
            }

            if (scenario.nextItemId <= maximumId)
                throw new ArgumentException("Next item ID must be greater than every initial item ID.");
        }

        private static IEnumerable<FactoryItem> EnumerateItems(FactoryNodeInitialState state)
        {
            if (state.outputItem != null) yield return state.outputItem;
            if (state.workItem != null) yield return state.workItem;
            if (state.waitingCoal != null) yield return state.waitingCoal;
            if (state.waitingCan != null) yield return state.waitingCan;
            foreach (FactoryBeltItem beltItem in state.beltItems)
                if (beltItem?.item != null) yield return beltItem.item;
        }

        private static void ValidateAcyclic(FactoryScenario scenario)
        {
            var adjacency = scenario.nodes.ToDictionary(node => node.nodeId, _ => new List<int>());
            foreach (FactoryConnection connection in scenario.connections)
                adjacency[connection.sourceNodeId].Add(connection.destinationNodeId);
            var visiting = new HashSet<int>();
            var visited = new HashSet<int>();
            foreach (int nodeId in adjacency.Keys)
                if (HasCycle(nodeId, adjacency, visiting, visited))
                    throw new ArgumentException("Connection cycles are outside the supported topology.");
        }

        private static bool HasCycle(int nodeId, Dictionary<int, List<int>> adjacency,
            HashSet<int> visiting, HashSet<int> visited)
        {
            if (visited.Contains(nodeId)) return false;
            if (!visiting.Add(nodeId)) return true;
            foreach (int destination in adjacency[nodeId])
                if (HasCycle(destination, adjacency, visiting, visited)) return true;
            visiting.Remove(nodeId);
            visited.Add(nodeId);
            return false;
        }

        private sealed class NodeState
        {
            public NodeState(FactoryNodeConfig config)
            {
                Config = config;
                Enabled = config.enabled;
            }

            public FactoryNodeConfig Config { get; }
            public bool Enabled { get; set; }
            public int WorkProgress { get; set; }
            public int FuelRemaining { get; set; }
            public int AppliedCharges { get; set; }
            public int SprayProgress { get; set; }
            public int ReservoirCharges { get; set; }
            public string ReservoirColorId { get; set; } = string.Empty;
            public FactoryItem OutputItem { get; set; }
            public FactoryItem WorkItem { get; set; }
            public FactoryItem WaitingCoal { get; set; }
            public FactoryItem WaitingCan { get; set; }
            public List<FactoryBeltItem> BeltItems { get; } = new List<FactoryBeltItem>();
            public SortedDictionary<string, int> SinkByColor { get; } =
                new SortedDictionary<string, int>(StringComparer.Ordinal);
            public List<long> ConsumedItemIds { get; } = new List<long>();
        }

        private readonly struct PortKey : IEquatable<PortKey>
        {
            public PortKey(int nodeId, int portId)
            {
                NodeId = nodeId;
                PortId = portId;
            }

            private int NodeId { get; }
            private int PortId { get; }
            public bool Equals(PortKey other) => NodeId == other.NodeId && PortId == other.PortId;
            public override bool Equals(object obj) => obj is PortKey other && Equals(other);
            public override int GetHashCode() => (NodeId * 397) ^ PortId;
        }

        private readonly struct TransferProposal
        {
            public TransferProposal(NodeState source, int sourcePortId, FactoryItem item,
                FactoryConnection connection)
            {
                Source = source;
                SourcePortId = sourcePortId;
                Item = item;
                Connection = connection;
            }

            public NodeState Source { get; }
            public int SourcePortId { get; }
            public FactoryItem Item { get; }
            public FactoryConnection Connection { get; }
        }
    }
}
