namespace FabriccaBellissima.Factory
{
    public sealed class SinkNodeFactory : IFactoryNodeFactory
    {
        public FactoryNodeKind Kind => FactoryNodeKind.Sink;
        public void Validate(FactoryNodeConfig config) { }
        public IFactoryNode Create(FactoryNodeConfig config) => new SinkNode(config);
    }
}
