using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class FactorySnapshotBuilder
    {
        public FactorySnapshot Build(long tick, FactoryScenario scenario, long nextItemId,
            FactoryCounters counters, IReadOnlyList<IFactoryNode> nodes, IReadOnlyList<FactoryEvent> events)
        {
            var snapshot = new FactorySnapshot
            {
                tick = tick,
                tickDurationMicroseconds = scenario.tickDurationMicroseconds,
                nextItemId = nextItemId,
                counters = counters.Clone()
            };
            for (int index = 0; index < nodes.Count; index++) snapshot.nodes.Add(nodes[index].CreateSnapshot());
            var connections = new List<FactoryConnection>(scenario.connections);
            connections.Sort(FactoryConnectionOrder.Compare);
            for (int index = 0; index < connections.Count; index++)
            {
                FactoryConnection connection = connections[index];
                snapshot.connections.Add(new FactoryConnection
                {
                    sourceNodeId = connection.sourceNodeId,
                    sourcePortId = connection.sourcePortId,
                    destinationNodeId = connection.destinationNodeId,
                    destinationPortId = connection.destinationPortId
                });
            }
            for (int index = 0; index < events.Count; index++) snapshot.events.Add(events[index]);
            return snapshot;
        }
    }
}
