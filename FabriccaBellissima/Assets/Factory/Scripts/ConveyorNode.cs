using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class ConveyorNode : FactoryNodeBase<ConveyorNodeConfig>
    {
        private readonly List<FactoryBeltItem> _items = new List<FactoryBeltItem>();

        public ConveyorNode(ConveyorNodeConfig config) : base(config) { }

        public override void LocalUpdate(FactoryTickContext context)
        {
            if (!Enabled || _items.Count == 0) return;
            _items[0].progressUnits = Math.Min(Config.lengthUnits,
                _items[0].progressUnits + Config.movementPerTick);
            for (int index = 1; index < _items.Count; index++)
            {
                FactoryBeltItem current = _items[index];
                int maximum = Math.Max(current.progressUnits,
                    _items[index - 1].progressUnits - Config.spacingUnits);
                current.progressUnits = Math.Min(maximum, current.progressUnits + Config.movementPerTick);
            }
        }

        public override bool TryGetOutput(out FactoryOutputProposal proposal)
        {
            proposal = default;
            if (!Enabled || _items.Count == 0 || _items[0].progressUnits < Config.lengthUnits) return false;
            proposal = new FactoryOutputProposal(this, FactoryPorts.Output, _items[0].item);
            return true;
        }

        public override bool CanAccept(int portId, FactoryItem item)
        {
            if (!Enabled || item == null || portId != FactoryPorts.Input || _items.Count >= Config.capacity)
                return false;
            return _items.Count == 0 || _items[_items.Count - 1].progressUnits >= Config.spacingUnits;
        }

        public override void Accept(int portId, FactoryItem item, FactoryTickContext context)
        {
            if (!CanAccept(portId, item)) RejectUnsupportedInput();
            _items.Add(new FactoryBeltItem { item = item, progressUnits = 0 });
        }

        public override FactoryItem RemoveOutput(long expectedItemId)
        {
            FactoryItem item = _items.Count == 0 ? null : _items[0].item;
            if (_items.Count > 0) _items.RemoveAt(0);
            return VerifyRemoved(item, expectedItemId);
        }

        public override bool SupportsInputPort(int portId) => portId == FactoryPorts.Input;
        public override bool SupportsOutputPort(int portId) => portId == FactoryPorts.Output;

        public override void ApplyInitialState(FactoryNodeInitialState state)
        {
            _items.Clear();
            for (int index = 0; index < state.beltItems.Count; index++) _items.Add(state.beltItems[index].Clone());
        }

        public override FactoryNodeSnapshot CreateSnapshot()
        {
            string status = _items.Count == 0 ? "Waiting for input" :
                _items[0].progressUnits >= Config.lengthUnits ? "Exit blocked" :
                $"Moving {_items.Count}/{Config.capacity}";
            FactoryNodeSnapshot snapshot = CreateCommonSnapshot(status);
            snapshot.lengthUnits = Config.lengthUnits;
            snapshot.movementPerTick = Config.movementPerTick;
            snapshot.capacity = Config.capacity;
            snapshot.spacingUnits = Config.spacingUnits;
            for (int index = 0; index < _items.Count; index++) snapshot.beltItems.Add(_items[index].Clone());
            return snapshot;
        }

        public override IEnumerable<FactoryItem> EnumerateOwnedItems()
        {
            for (int index = 0; index < _items.Count; index++) yield return _items[index].item;
        }
    }
}
