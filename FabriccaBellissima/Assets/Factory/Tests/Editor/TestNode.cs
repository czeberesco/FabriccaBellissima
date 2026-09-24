using System.Collections.Generic;

namespace FabriccaBellissima.Factory.Tests
{
    public sealed class TestNode : FactoryNodeBase
    {
        private int _ticks;

        public TestNode(FactoryNodeConfig config) : base(config) { }
        public override void LocalUpdate(FactoryTickContext context) { if (Enabled) _ticks++; }
        public override bool TryGetOutput(out FactoryOutputProposal proposal) { proposal = default; return false; }
        public override bool CanAccept(int portId, FactoryItem item) => false;
        public override void Accept(int portId, FactoryItem item, FactoryTickContext context) => RejectUnsupportedInput();
        public override FactoryItem RemoveOutput(long expectedItemId) => null;
        public override bool SupportsInputPort(int portId) => false;
        public override bool SupportsOutputPort(int portId) => false;
        public override void ApplyInitialState(FactoryNodeInitialState initialState) { }
        public override IEnumerable<FactoryItem> EnumerateOwnedItems() { yield break; }

        public override FactoryNodeSnapshot CreateSnapshot()
        {
            FactoryNodeSnapshot snapshot = CreateCommonSnapshot("Extension active");
            snapshot.workProgress = _ticks;
            return snapshot;
        }
    }
}
