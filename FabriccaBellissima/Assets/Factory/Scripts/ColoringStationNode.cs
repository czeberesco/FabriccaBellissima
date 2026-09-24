using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class ColoringStationNode : FactoryNodeBase<ColoringStationNodeConfig>
    {
        private int _appliedCharges;
        private int _sprayProgress;
        private int _reservoirCharges;
        private string _reservoirColorId = string.Empty;
        private FactoryItem _workItem;
        private FactoryItem _waitingCan;

        public ColoringStationNode(ColoringStationNodeConfig config) : base(config) { }

        public override void LocalUpdate(FactoryTickContext context)
        {
            if (!Enabled) return;
            if (_reservoirCharges == 0 && _waitingCan != null)
            {
                FactoryItem can = _waitingCan;
                _waitingCan = null;
                _reservoirCharges = can.charges;
                _reservoirColorId = can.colorId;
                context.Counters.cansOpened++;
                context.AddEvent(FactoryEventKind.CanOpened, NodeId, FactoryPorts.ColoringCan, can,
                    FactoryResourceType.PaintCan, FactoryResourceType.None, amount: can.charges);
            }
            if (_workItem == null || _workItem.resource != FactoryResourceType.IronBar || _reservoirCharges <= 0)
                return;
            _sprayProgress++;
            if (_sprayProgress < Config.sprayTicks) return;
            _sprayProgress = 0;
            _reservoirCharges--;
            _appliedCharges++;
            context.Counters.chargesApplied++;
            context.AddEvent(FactoryEventKind.ChargeApplied, NodeId, FactoryPorts.ColoringBar, _workItem,
                FactoryResourceType.IronBar, FactoryResourceType.IronBar, amount: _appliedCharges);
            if (_appliedCharges < Config.chargesPerBar) return;
            _workItem.resource = FactoryResourceType.PaintedIronBar;
            _workItem.colorId = Config.colorId;
            context.Counters.paintedBarsMade++;
            context.AddEvent(FactoryEventKind.Transformed, NodeId, FactoryPorts.ColoringOutput, _workItem,
                FactoryResourceType.IronBar, FactoryResourceType.PaintedIronBar);
        }

        public override bool TryGetOutput(out FactoryOutputProposal proposal)
        {
            proposal = default;
            if (!Enabled || _workItem == null || _workItem.resource != FactoryResourceType.PaintedIronBar)
                return false;
            proposal = new FactoryOutputProposal(this, FactoryPorts.ColoringOutput, _workItem);
            return true;
        }

        public override bool CanAccept(int portId, FactoryItem item)
        {
            if (!Enabled || item == null) return false;
            if (portId == FactoryPorts.ColoringBar)
                return item.resource == FactoryResourceType.IronBar && _workItem == null;
            return portId == FactoryPorts.ColoringCan && item.resource == FactoryResourceType.PaintCan &&
                   _waitingCan == null && item.charges > 0 &&
                   string.Equals(item.colorId, Config.colorId, StringComparison.Ordinal);
        }

        public override void Accept(int portId, FactoryItem item, FactoryTickContext context)
        {
            if (!CanAccept(portId, item)) RejectUnsupportedInput();
            if (portId == FactoryPorts.ColoringBar)
            {
                _workItem = item;
                _appliedCharges = 0;
                _sprayProgress = 0;
            }
            else _waitingCan = item;
        }

        public override FactoryItem RemoveOutput(long expectedItemId)
        {
            FactoryItem item = _workItem;
            _workItem = null;
            _appliedCharges = 0;
            _sprayProgress = 0;
            return VerifyRemoved(item, expectedItemId);
        }

        public override bool SupportsInputPort(int portId) =>
            portId == FactoryPorts.ColoringBar || portId == FactoryPorts.ColoringCan;
        public override bool SupportsOutputPort(int portId) => portId == FactoryPorts.ColoringOutput;

        public override void ApplyInitialState(FactoryNodeInitialState state)
        {
            _appliedCharges = state.appliedCharges;
            _sprayProgress = state.sprayProgress;
            _reservoirCharges = state.reservoirCharges;
            _reservoirColorId = state.reservoirColorId ?? string.Empty;
            _workItem = Clone(state.workItem);
            _waitingCan = Clone(state.waitingCan);
        }

        public override FactoryNodeSnapshot CreateSnapshot()
        {
            string status;
            if (_workItem != null && _workItem.resource == FactoryResourceType.PaintedIronBar) status = "Output blocked";
            else if (_workItem == null) status = "Waiting for bar";
            else if (_reservoirCharges == 0 && _waitingCan == null) status = "Waiting for paint";
            else status = "Spraying";
            FactoryNodeSnapshot snapshot = CreateCommonSnapshot(status);
            snapshot.configuredColorId = Config.colorId;
            snapshot.chargesPerBar = Config.chargesPerBar;
            snapshot.sprayTicks = Config.sprayTicks;
            snapshot.workProgress = _sprayProgress;
            snapshot.workRequired = Config.sprayTicks;
            snapshot.reservoirCharges = _reservoirCharges;
            snapshot.reservoirColorId = _reservoirColorId;
            snapshot.appliedCharges = _appliedCharges;
            snapshot.sprayProgress = _sprayProgress;
            snapshot.workItem = Clone(_workItem);
            snapshot.waitingCan = Clone(_waitingCan);
            return snapshot;
        }

        public override IEnumerable<FactoryItem> EnumerateOwnedItems()
        {
            if (_workItem != null) yield return _workItem;
            if (_waitingCan != null) yield return _waitingCan;
        }
    }
}
