using System;

namespace FabriccaBellissima.Factory
{
    public sealed class ColoringStationNodeFactory : IFactoryNodeFactory
    {
        public FactoryNodeKind Kind => FactoryNodeKind.ColoringStation;
        public void Validate(FactoryNodeConfig config)
        {
            if (config.sprayTicks <= 0 || config.chargesPerBar <= 0 || string.IsNullOrEmpty(config.colorId))
                throw new ArgumentException($"Coloring station {config.nodeId} configuration is invalid.");
        }
        public IFactoryNode Create(FactoryNodeConfig config) => new ColoringStationNode(config);
    }
}
