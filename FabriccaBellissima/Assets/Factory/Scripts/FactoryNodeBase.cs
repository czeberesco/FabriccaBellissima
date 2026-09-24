using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public abstract class FactoryNodeBase<TConfig> : IFactoryNode where TConfig : FactoryNodeConfig
    {
        protected FactoryNodeBase(TConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Enabled = config.enabled;
        }

        public TConfig Config { get; }
        FactoryNodeConfig IFactoryNode.Config => Config;
        public int NodeId => Config.nodeId;
        public bool Enabled { get; set; }

        public abstract void LocalUpdate(FactoryTickContext context);
        public abstract bool TryGetOutput(out FactoryOutputProposal proposal);
        public abstract bool CanAccept(int portId, FactoryItem item);
        public abstract void Accept(int portId, FactoryItem item, FactoryTickContext context);
        public abstract FactoryItem RemoveOutput(long expectedItemId);
        public abstract bool SupportsInputPort(int portId);
        public abstract bool SupportsOutputPort(int portId);
        public abstract void ApplyInitialState(FactoryNodeInitialState initialState);
        public abstract FactoryNodeSnapshot CreateSnapshot();
        public abstract IEnumerable<FactoryItem> EnumerateOwnedItems();

        protected FactoryNodeSnapshot CreateCommonSnapshot(string status)
        {
            return new FactoryNodeSnapshot
            {
                nodeId = Config.nodeId,
                displayName = Config.displayName,
                kind = Config.kind,
                enabled = Enabled,
                status = Enabled ? status : "Disabled"
            };
        }

        protected static FactoryItem Clone(FactoryItem item) => item?.Clone();

        protected static FactoryItem VerifyRemoved(FactoryItem item, long expectedItemId)
        {
            if (item == null || item.id != expectedItemId)
                throw new InvalidOperationException("Proposed transfer item ownership changed before commit.");
            return item;
        }

        protected static void RejectUnsupportedInput() =>
            throw new InvalidOperationException("Destination accepted through an unsupported port.");
    }
}
