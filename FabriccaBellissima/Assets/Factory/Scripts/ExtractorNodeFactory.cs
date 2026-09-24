using System;

namespace FabriccaBellissima.Factory
{
    public sealed class ExtractorNodeFactory : IFactoryNodeFactory
    {
        public FactoryNodeKind Kind => FactoryNodeKind.Extractor;

        public void Validate(FactoryNodeConfig config)
        {
            if (config.durationTicks <= 0)
                throw new ArgumentException($"Extractor {config.nodeId} duration must be positive.");
            if (config.resource != FactoryResourceType.IronOre && config.resource != FactoryResourceType.Coal &&
                config.resource != FactoryResourceType.PaintCan)
                throw new ArgumentException($"Extractor {config.nodeId} has an unsupported resource.");
            if (config.resource == FactoryResourceType.PaintCan &&
                (config.paintCanCharges <= 0 || string.IsNullOrEmpty(config.colorId)))
                throw new ArgumentException($"Paint extractor {config.nodeId} needs charges and a color.");
        }

        public IFactoryNode Create(FactoryNodeConfig config) => new ExtractorNode(config);
    }
}
