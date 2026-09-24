using System;

namespace FabriccaBellissima.Factory
{
    public sealed class ColoringStationNodeFactory : FactoryNodeFactory<ColoringStationNodeConfig>
    {
        public override FactoryNodeKind Kind => FactoryNodeKind.ColoringStation;
        protected override void Validate(ColoringStationNodeConfig config)
        {
            if (config.sprayTicks <= 0 || config.chargesPerBar <= 0 || string.IsNullOrEmpty(config.colorId))
                throw new ArgumentException($"Coloring station {config.nodeId} configuration is invalid.");
        }
        protected override IFactoryNode Create(ColoringStationNodeConfig config) => new ColoringStationNode(config);
    }
}
