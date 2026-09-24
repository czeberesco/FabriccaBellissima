using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class SinkNode : FactoryNodeBase
    {
        private readonly SortedDictionary<string, int> _countsByColor =
            new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly List<long> _consumedItemIds = new List<long>();

        public SinkNode(FactoryNodeConfig config) : base(config) { }
        public override void LocalUpdate(FactoryTickContext context) { }
        public override bool TryGetOutput(out FactoryOutputProposal proposal) { proposal = default; return false; }

        public override bool CanAccept(int portId, FactoryItem item) => Enabled && portId == FactoryPorts.Input &&
            item != null && item.resource == FactoryResourceType.PaintedIronBar &&
            string.Equals(item.colorId, Config.colorId, StringComparison.Ordinal);

        public override void Accept(int portId, FactoryItem item, FactoryTickContext context)
        {
            if (!CanAccept(portId, item)) RejectUnsupportedInput();
            _consumedItemIds.Add(item.id);
            if (!_countsByColor.ContainsKey(item.colorId)) _countsByColor.Add(item.colorId, 0);
            _countsByColor[item.colorId]++;
            context.Counters.sinkConsumed++;
            context.AddEvent(FactoryEventKind.SinkConsumed, NodeId, FactoryPorts.Input, item,
                FactoryResourceType.PaintedIronBar, FactoryResourceType.None);
        }

        public override FactoryItem RemoveOutput(long expectedItemId) =>
            throw new InvalidOperationException("A sink has no output.");
        public override bool SupportsInputPort(int portId) => portId == FactoryPorts.Input;
        public override bool SupportsOutputPort(int portId) => false;
        public override void ApplyInitialState(FactoryNodeInitialState state) { }

        public override FactoryNodeSnapshot CreateSnapshot()
        {
            FactoryNodeSnapshot snapshot = CreateCommonSnapshot($"Consumed {_consumedItemIds.Count}");
            snapshot.sinkTotal = _consumedItemIds.Count;
            foreach (KeyValuePair<string, int> entry in _countsByColor)
            {
                snapshot.sinkColors.Add(entry.Key);
                snapshot.sinkColorCounts.Add(entry.Value);
            }
            snapshot.consumedItemIds.AddRange(_consumedItemIds);
            return snapshot;
        }

        public override IEnumerable<FactoryItem> EnumerateOwnedItems() { yield break; }
    }
}
