using System;

namespace FabriccaBellissima.Factory
{
    public sealed class FurnaceNodeFactory : IFactoryNodeFactory
    {
        public FactoryNodeKind Kind => FactoryNodeKind.Furnace;
        public void Validate(FactoryNodeConfig config)
        {
            if (config.burnTicks <= 0 || config.smeltTicks <= 0)
                throw new ArgumentException($"Furnace {config.nodeId} durations must be positive.");
        }
        public IFactoryNode Create(FactoryNodeConfig config) => new FurnaceNode(config);
    }
}
