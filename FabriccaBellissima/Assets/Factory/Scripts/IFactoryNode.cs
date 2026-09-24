using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public interface IFactoryNode
    {
        FactoryNodeConfig Config { get; }
        int NodeId { get; }
        bool Enabled { get; set; }
        void LocalUpdate(FactoryTickContext context);
        bool TryGetOutput(out FactoryOutputProposal proposal);
        bool CanAccept(int portId, FactoryItem item);
        void Accept(int portId, FactoryItem item, FactoryTickContext context);
        FactoryItem RemoveOutput(long expectedItemId);
        bool SupportsInputPort(int portId);
        bool SupportsOutputPort(int portId);
        void ApplyInitialState(FactoryNodeInitialState initialState);
        FactoryNodeSnapshot CreateSnapshot();
        IEnumerable<FactoryItem> EnumerateOwnedItems();
    }
}
