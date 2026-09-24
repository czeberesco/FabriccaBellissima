using System;

namespace FabriccaBellissima.Factory
{
    public sealed class FurnaceNodeFactory : FactoryNodeFactory<FurnaceNodeConfig>
    {
        public override FactoryNodeKind Kind => FactoryNodeKind.Furnace;
        protected override void Validate(FurnaceNodeConfig config)
        {
            if (config.burnTicks <= 0 || config.smeltTicks <= 0)
                throw new ArgumentException($"Furnace {config.nodeId} durations must be positive.");
        }
        protected override IFactoryNode Create(FurnaceNodeConfig config) => new FurnaceNode(config);
    }
}
