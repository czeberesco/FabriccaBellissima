using System;
using System.Collections.Generic;
using System.Linq;

namespace FabriccaBellissima.Factory
{
    public sealed class FactorySimulation : IFactorySimulation
    {
        private readonly FactoryScenario _scenario;
        private readonly FactoryNodeFactoryRegistry _nodeFactories;
        private readonly FactoryScenarioValidator _validator;
        private readonly FactoryTransferResolver _transferResolver;
        private readonly FactorySnapshotBuilder _snapshotBuilder;
        private readonly List<IFactoryNode> _nodes = new List<IFactoryNode>();
        private readonly Dictionary<int, IFactoryNode> _nodesById = new Dictionary<int, IFactoryNode>();
        private readonly Dictionary<PortKey, FactoryConnection> _connections = new Dictionary<PortKey, FactoryConnection>();
        private readonly List<FactoryEvent> _events = new List<FactoryEvent>();
        private FactoryCounters _counters;
        private FactoryTickContext _tickContext;

        public FactorySimulation(FactoryScenario scenario)
            : this(scenario, FactoryNodeFactoryRegistry.CreateDefault(), new FactoryTransferResolver(),
                new FactorySnapshotBuilder()) { }

        public FactorySimulation(FactoryScenario scenario, FactoryNodeFactoryRegistry nodeFactories,
            FactoryTransferResolver transferResolver, FactorySnapshotBuilder snapshotBuilder)
        {
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            _nodeFactories = nodeFactories ?? throw new ArgumentNullException(nameof(nodeFactories));
            _transferResolver = transferResolver ?? throw new ArgumentNullException(nameof(transferResolver));
            _snapshotBuilder = snapshotBuilder ?? throw new ArgumentNullException(nameof(snapshotBuilder));
            _validator = new FactoryScenarioValidator(_nodeFactories);
            _validator.Validate(_scenario);
            BuildInitialState();
        }

        public long Tick { get; private set; }
        public float TickDurationSeconds => _scenario.tickDurationMicroseconds / 1000000f;
        public FactorySnapshot CurrentSnapshot => _snapshotBuilder.Build(Tick, _scenario,
            _tickContext.NextItemId, _counters, _nodes, _events);
        public IReadOnlyList<FactoryNodeConfig> NodeConfigs => _scenario.nodes;
        public IReadOnlyList<FactoryConnection> Connections => _scenario.connections;
        public IReadOnlyList<FactoryEvent> LastEvents => _events;

        public static FactorySimulation FromJson(string json) => new FactorySimulation(FactoryScenario.FromJson(json));

        public void StepOneTick()
        {
            Tick++;
            _events.Clear();
            _tickContext.BeginTick(Tick);
            for (int index = 0; index < _nodes.Count; index++) _nodes[index].LocalUpdate(_tickContext);
            _transferResolver.Resolve(_nodes, _nodesById, _connections, _tickContext);
        }

        public void StepTicks(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Tick count cannot be negative.");
            for (int index = 0; index < count; index++) StepOneTick();
        }

        public void ResetSimulation() => BuildInitialState();

        public void SetNodeEnabled(int nodeId, bool enabled)
        {
            if (!_nodesById.TryGetValue(nodeId, out IFactoryNode node))
                throw new ArgumentException($"Unknown node ID {nodeId}.", nameof(nodeId));
            node.Enabled = enabled;
        }

        public bool GetNodeEnabled(int nodeId) => _nodesById[nodeId].Enabled;
        public string ExportCanonicalSnapshot(bool pretty = false) => CurrentSnapshot.ToCanonicalJson(pretty);

        private void BuildInitialState()
        {
            Tick = _scenario.initialTick;
            _counters = new FactoryCounters();
            _events.Clear();
            _tickContext = new FactoryTickContext(_scenario.nextItemId, _counters, _events);
            _nodes.Clear();
            _nodesById.Clear();
            _connections.Clear();
            foreach (FactoryNodeConfig config in _scenario.nodes.OrderBy(node => node.nodeId))
            {
                IFactoryNode node = _nodeFactories.Create(config);
                _nodes.Add(node);
                _nodesById.Add(config.nodeId, node);
            }
            for (int index = 0; index < _scenario.connections.Count; index++)
            {
                FactoryConnection connection = _scenario.connections[index];
                _connections.Add(new PortKey(connection.sourceNodeId, connection.sourcePortId), connection);
            }
            for (int index = 0; index < _scenario.initialStates.Count; index++)
            {
                FactoryNodeInitialState initial = _scenario.initialStates[index];
                _nodesById[initial.nodeId].ApplyInitialState(initial);
            }
        }
    }
}
