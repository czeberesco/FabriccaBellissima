namespace FabriccaBellissima.Factory.Tests
{
    public sealed class TestNodeConfig : FactoryNodeConfig
    {
        public TestNodeConfig() : base(TestNodeFactory.TestKind) { }
    }
}
