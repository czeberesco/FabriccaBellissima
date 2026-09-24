namespace FabriccaBellissima.Factory.Tests
{
    public sealed class TestNodeFactory : IFactoryNodeFactory
    {
        public const FactoryNodeKind TestKind = (FactoryNodeKind)999;
        public FactoryNodeKind Kind => TestKind;
        public void Validate(FactoryNodeConfig config) { }
        public IFactoryNode Create(FactoryNodeConfig config) => new TestNode(config);
    }
}
