using System;

namespace FabriccaBellissima.Factory
{
    public sealed class ConveyorNodeFactory : IFactoryNodeFactory
    {
        public FactoryNodeKind Kind => FactoryNodeKind.Conveyor;

        public void Validate(FactoryNodeConfig config)
        {
            if (config.capacity <= 0 || config.lengthUnits < config.capacity || config.movementPerTick <= 0 ||
                config.spacingUnits != Math.Max(1, config.lengthUnits / config.capacity))
                throw new ArgumentException($"Conveyor {config.nodeId} has invalid length, speed, capacity, or spacing.");
        }

        public IFactoryNode Create(FactoryNodeConfig config) => new ConveyorNode(config);
    }
}
