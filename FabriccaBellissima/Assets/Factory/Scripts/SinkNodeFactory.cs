namespace FabriccaBellissima.Factory
{
    public sealed class SinkNodeFactory : FactoryNodeFactory<SinkNodeConfig>
    {
        public override FactoryNodeKind Kind => FactoryNodeKind.Sink;
        protected override void Validate(SinkNodeConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.acceptedColorId))
                throw new System.ArgumentException($"Sink {config.nodeId} accepted color cannot be empty.");
        }
        protected override IFactoryNode Create(SinkNodeConfig config) => new SinkNode(config);
    }
}
