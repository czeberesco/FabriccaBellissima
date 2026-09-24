using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryTransferResolver
    {
        public void Resolve(IReadOnlyList<IFactoryNode> nodes,
            IReadOnlyDictionary<int, IFactoryNode> nodesById,
            IReadOnlyDictionary<PortKey, FactoryConnection> connections,
            FactoryTickContext context)
        {
            var proposals = new List<FactoryOutputProposal>();
            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index].TryGetOutput(out FactoryOutputProposal proposal) &&
                    connections.ContainsKey(new PortKey(proposal.Source.NodeId, proposal.SourcePortId)))
                    proposals.Add(proposal);
            }
            proposals.Sort(Compare);
            for (int index = 0; index < proposals.Count; index++)
            {
                FactoryOutputProposal proposal = proposals[index];
                FactoryConnection connection = connections[new PortKey(proposal.Source.NodeId, proposal.SourcePortId)];
                IFactoryNode destination = nodesById[connection.destinationNodeId];
                if (!destination.CanAccept(connection.destinationPortId, proposal.Item)) continue;
                FactoryItem transferred = proposal.Source.RemoveOutput(proposal.Item.id);
                context.AddEvent(FactoryEventKind.Transferred, proposal.Source.NodeId, proposal.SourcePortId,
                    transferred, transferred.resource, transferred.resource, connection.destinationNodeId,
                    connection.destinationPortId);
                destination.Accept(connection.destinationPortId, transferred, context);
            }
        }

        private static int Compare(FactoryOutputProposal left, FactoryOutputProposal right)
        {
            int nodeComparison = left.Source.NodeId.CompareTo(right.Source.NodeId);
            return nodeComparison != 0 ? nodeComparison : left.SourcePortId.CompareTo(right.SourcePortId);
        }
    }
}
