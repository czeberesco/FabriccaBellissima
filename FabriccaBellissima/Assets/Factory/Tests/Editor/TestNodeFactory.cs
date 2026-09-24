namespace FabriccaBellissima.Factory.Tests
{
    public sealed class TestNodeFactory : FactoryNodeFactory<TestNodeConfig>
    {
        public const FactoryNodeKind TestKind = (FactoryNodeKind)999;
        public override FactoryNodeKind Kind => TestKind;
        protected override void Validate(TestNodeConfig config) { }
        protected override IFactoryNode Create(TestNodeConfig config) => new TestNode(config);
    }
}
