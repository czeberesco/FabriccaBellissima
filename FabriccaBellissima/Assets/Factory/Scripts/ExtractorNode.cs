using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class ExtractorNode : FactoryNodeBase
    {
        private int _workProgress;
        private FactoryItem _outputItem;

        public ExtractorNode(FactoryNodeConfig config) : base(config) { }

        public override void LocalUpdate(FactoryTickContext context)
        {
            if (!Enabled || _outputItem != null) return;
            _workProgress++;
            if (_workProgress < Config.durationTicks) return;
            _workProgress = 0;
            _outputItem = new FactoryItem
            {
                id = context.AllocateItemId(),
                resource = Config.resource,
                colorId = Config.resource == FactoryResourceType.PaintCan ? Config.colorId : string.Empty,
                charges = Config.resource == FactoryResourceType.PaintCan ? Config.paintCanCharges : 0
            };
            context.IncrementGenerated(Config.resource);
            context.AddEvent(FactoryEventKind.Generated, NodeId, FactoryPorts.Output, _outputItem,
                FactoryResourceType.None, _outputItem.resource);
        }

        public override bool TryGetOutput(out FactoryOutputProposal proposal)
        {
            proposal = default;
            if (!Enabled || _outputItem == null) return false;
            proposal = new FactoryOutputProposal(this, FactoryPorts.Output, _outputItem);
            return true;
        }

        public override bool CanAccept(int portId, FactoryItem item) => false;
        public override void Accept(int portId, FactoryItem item, FactoryTickContext context) => RejectUnsupportedInput();

        public override FactoryItem RemoveOutput(long expectedItemId)
        {
            FactoryItem item = _outputItem;
            _outputItem = null;
            return VerifyRemoved(item, expectedItemId);
        }

        public override bool SupportsInputPort(int portId) => false;
        public override bool SupportsOutputPort(int portId) => portId == FactoryPorts.Output;

        public override void ApplyInitialState(FactoryNodeInitialState state)
        {
            _workProgress = state.workProgress;
            _outputItem = Clone(state.outputItem);
        }

        public override FactoryNodeSnapshot CreateSnapshot()
        {
            FactoryNodeSnapshot snapshot = CreateCommonSnapshot(_outputItem != null ? "Output blocked" : "Extracting");
            snapshot.workProgress = _workProgress;
            snapshot.workRequired = Config.durationTicks;
            snapshot.outputItem = Clone(_outputItem);
            return snapshot;
        }

        public override IEnumerable<FactoryItem> EnumerateOwnedItems()
        {
            if (_outputItem != null) yield return _outputItem;
        }
    }
}
