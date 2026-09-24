using System;
using System.Collections.Generic;
using System.Linq;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryScenarioValidator
    {
        private readonly FactoryNodeFactoryRegistry _registry;

        public FactoryScenarioValidator(FactoryNodeFactoryRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Validate(FactoryScenario scenario)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (scenario.formatVersion != 1)
                throw new ArgumentException($"Unsupported scenario version {scenario.formatVersion}.");
            if (scenario.tickDurationMicroseconds <= 0)
                throw new ArgumentException("Tick duration in microseconds must be positive.");
            if (scenario.nextItemId <= 0) throw new ArgumentException("Next item ID must be positive.");
            if (scenario.nodes == null || scenario.nodes.Count == 0)
                throw new ArgumentException("Scenario needs nodes.");
            if (scenario.connections == null || scenario.initialStates == null)
                throw new ArgumentException("Scenario collections cannot be null.");

            var ids = new HashSet<int>();
            var nodes = new Dictionary<int, IFactoryNode>();
            for (int index = 0; index < scenario.nodes.Count; index++)
            {
                FactoryNodeConfig config = scenario.nodes[index];
                if (config == null || config.nodeId <= 0 || !ids.Add(config.nodeId))
                    throw new ArgumentException("Node IDs must be positive and unique.");
                if (config.configurationVersion != 1)
                    throw new ArgumentException($"Node {config.nodeId} has unsupported configuration version {config.configurationVersion}.");
                if (string.IsNullOrWhiteSpace(config.displayName))
                    throw new ArgumentException($"Node {config.nodeId} needs a display name.");
                _registry.Validate(config);
                nodes.Add(config.nodeId, _registry.Create(config));
            }

            ValidateConnections(scenario, ids, nodes);
            ValidateAcyclic(scenario);
            ValidateInitialState(scenario, ids);
        }

        private static void ValidateConnections(FactoryScenario scenario, HashSet<int> ids,
            IReadOnlyDictionary<int, IFactoryNode> nodes)
        {
            var sources = new HashSet<PortKey>();
            var destinations = new HashSet<PortKey>();
            for (int index = 0; index < scenario.connections.Count; index++)
            {
                FactoryConnection connection = scenario.connections[index];
                if (connection == null || !ids.Contains(connection.sourceNodeId) ||
                    !ids.Contains(connection.destinationNodeId))
                    throw new ArgumentException("Every connection must reference existing nodes.");
                if (connection.sourceNodeId == connection.destinationNodeId)
                    throw new ArgumentException("Self connections are not supported.");
                if (!sources.Add(new PortKey(connection.sourceNodeId, connection.sourcePortId)))
                    throw new ArgumentException("An output port can have only one connection.");
                if (!destinations.Add(new PortKey(connection.destinationNodeId, connection.destinationPortId)))
                    throw new ArgumentException("An input port can have only one connection.");
                if (!nodes[connection.sourceNodeId].SupportsOutputPort(connection.sourcePortId))
                    throw new ArgumentException($"Node {connection.sourceNodeId} has no output port {connection.sourcePortId}.");
                if (!nodes[connection.destinationNodeId].SupportsInputPort(connection.destinationPortId))
                    throw new ArgumentException($"Node {connection.destinationNodeId} has no input port {connection.destinationPortId}.");
            }
        }

        private static void ValidateInitialState(FactoryScenario scenario, HashSet<int> nodeIds)
        {
            var stateNodes = new HashSet<int>();
            var itemIds = new HashSet<long>();
            long maximumId = 0;
            for (int index = 0; index < scenario.initialStates.Count; index++)
            {
                FactoryNodeInitialState state = scenario.initialStates[index];
                if (state == null || !nodeIds.Contains(state.nodeId) || !stateNodes.Add(state.nodeId))
                    throw new ArgumentException("Initial states must reference unique existing nodes.");
                foreach (FactoryItem item in EnumerateItems(state))
                {
                    if (item.id <= 0 || !itemIds.Add(item.id))
                        throw new ArgumentException("Initial item IDs must be positive and globally unique.");
                    maximumId = Math.Max(maximumId, item.id);
                }
                FactoryNodeConfig config = scenario.nodes.First(node => node.nodeId == state.nodeId);
                if (config is ConveyorNodeConfig conveyor)
                {
                    if (state.beltItems.Count > conveyor.capacity)
                        throw new ArgumentException($"Initial belt {state.nodeId} exceeds capacity.");
                    if (state.beltItems.Any(item => item.progressUnits < 0 || item.progressUnits > conveyor.lengthUnits))
                        throw new ArgumentException($"Initial belt {state.nodeId} progress is out of range.");
                }
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
            for (int index = 0; index < state.beltItems.Count; index++)
                if (state.beltItems[index]?.item != null) yield return state.beltItems[index].item;
        }

        private static void ValidateAcyclic(FactoryScenario scenario)
        {
            var adjacency = scenario.nodes.ToDictionary(node => node.nodeId, _ => new List<int>());
            for (int index = 0; index < scenario.connections.Count; index++)
                adjacency[scenario.connections[index].sourceNodeId].Add(scenario.connections[index].destinationNodeId);
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
            List<int> destinations = adjacency[nodeId];
            for (int index = 0; index < destinations.Count; index++)
                if (HasCycle(destinations[index], adjacency, visiting, visited)) return true;
            visiting.Remove(nodeId);
            visited.Add(nodeId);
            return false;
        }
    }
}
