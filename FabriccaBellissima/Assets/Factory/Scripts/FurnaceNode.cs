using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class FurnaceNode : FactoryNodeBase<FurnaceNodeConfig>
    {
        private int _workProgress;
        private int _fuelRemaining;
        private FactoryItem _workItem;
        private FactoryItem _waitingCoal;

        public FurnaceNode(FurnaceNodeConfig config) : base(config) { }

        public override void LocalUpdate(FactoryTickContext context)
        {
            if (!Enabled) return;
            if (_fuelRemaining == 0 && _waitingCoal != null)
            {
                FactoryItem coal = _waitingCoal;
                _waitingCoal = null;
                _fuelRemaining = Config.burnTicks;
                context.Counters.coalConsumed++;
                context.AddEvent(FactoryEventKind.CoalIgnited, NodeId, FactoryPorts.FurnaceFuel, coal,
                    FactoryResourceType.Coal, FactoryResourceType.None, amount: Config.burnTicks);
            }
            if (_fuelRemaining <= 0) return;
            if (_workItem != null && _workItem.resource == FactoryResourceType.IronOre)
            {
                _workProgress++;
                if (_workProgress >= Config.smeltTicks)
                {
                    _workItem.resource = FactoryResourceType.IronBar;
                    _workProgress = Config.smeltTicks;
                    context.Counters.ironBarsMade++;
                    context.AddEvent(FactoryEventKind.Transformed, NodeId, FactoryPorts.FurnaceOutput,
                        _workItem, FactoryResourceType.IronOre, FactoryResourceType.IronBar);
                }
            }
            _fuelRemaining--;
        }

        public override bool TryGetOutput(out FactoryOutputProposal proposal)
        {
            proposal = default;
            if (!Enabled || _workItem == null || _workItem.resource != FactoryResourceType.IronBar) return false;
            proposal = new FactoryOutputProposal(this, FactoryPorts.FurnaceOutput, _workItem);
            return true;
        }

        public override bool CanAccept(int portId, FactoryItem item)
        {
            if (!Enabled || item == null) return false;
            if (portId == FactoryPorts.FurnaceOre)
                return item.resource == FactoryResourceType.IronOre && _workItem == null;
            return portId == FactoryPorts.FurnaceFuel && item.resource == FactoryResourceType.Coal &&
                   _waitingCoal == null;
        }

        public override void Accept(int portId, FactoryItem item, FactoryTickContext context)
        {
            if (!CanAccept(portId, item)) RejectUnsupportedInput();
            if (portId == FactoryPorts.FurnaceOre)
            {
                _workItem = item;
                _workProgress = 0;
            }
            else _waitingCoal = item;
        }

        public override FactoryItem RemoveOutput(long expectedItemId)
        {
            FactoryItem item = _workItem;
            _workItem = null;
            _workProgress = 0;
            return VerifyRemoved(item, expectedItemId);
        }

        public override bool SupportsInputPort(int portId) =>
            portId == FactoryPorts.FurnaceOre || portId == FactoryPorts.FurnaceFuel;
        public override bool SupportsOutputPort(int portId) => portId == FactoryPorts.FurnaceOutput;

        public override void ApplyInitialState(FactoryNodeInitialState state)
        {
            _workProgress = state.workProgress;
            _fuelRemaining = state.fuelRemaining;
            _workItem = Clone(state.workItem);
            _waitingCoal = Clone(state.waitingCoal);
        }

        public override FactoryNodeSnapshot CreateSnapshot()
        {
            string status;
            if (_workItem != null && _workItem.resource == FactoryResourceType.IronBar) status = "Output blocked";
            else if (_workItem == null) status = _fuelRemaining > 0 ? "Idle (fuel burning)" : "Waiting for ore";
            else if (_fuelRemaining == 0 && _waitingCoal == null) status = "Waiting for fuel";
            else status = "Smelting";
            FactoryNodeSnapshot snapshot = CreateCommonSnapshot(status);
            snapshot.burnTicks = Config.burnTicks;
            snapshot.workProgress = _workProgress;
            snapshot.workRequired = Config.smeltTicks;
            snapshot.fuelRemaining = _fuelRemaining;
            snapshot.workItem = Clone(_workItem);
            snapshot.waitingCoal = Clone(_waitingCoal);
            return snapshot;
        }

        public override IEnumerable<FactoryItem> EnumerateOwnedItems()
        {
            if (_workItem != null) yield return _workItem;
            if (_waitingCoal != null) yield return _waitingCoal;
        }
    }
}
