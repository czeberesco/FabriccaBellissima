using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public abstract class FactoryNodeBase : IFactoryNode
    {
        protected FactoryNodeBase(FactoryNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Enabled = config.enabled;
        }

        public FactoryNodeConfig Config { get; }
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
                status = Enabled ? status : "Disabled",
                configuredResource = Config.resource,
                configuredColorId = Config.colorId,
                durationTicks = Config.durationTicks,
                paintCanCharges = Config.paintCanCharges,
                burnTicks = Config.burnTicks,
                chargesPerBar = Config.chargesPerBar,
                sprayTicks = Config.sprayTicks,
                lengthUnits = Config.lengthUnits,
                movementPerTick = Config.movementPerTick,
                capacity = Config.capacity,
                spacingUnits = Config.spacingUnits
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
