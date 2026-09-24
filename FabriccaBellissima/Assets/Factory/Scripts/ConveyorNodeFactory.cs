using System;

namespace FabriccaBellissima.Factory
{
    public sealed class ConveyorNodeFactory : FactoryNodeFactory<ConveyorNodeConfig>
    {
        public override FactoryNodeKind Kind => FactoryNodeKind.Conveyor;

        protected override void Validate(ConveyorNodeConfig config)
        {
            if (config.capacity <= 0 || config.lengthUnits < config.capacity || config.movementPerTick <= 0 ||
                config.spacingUnits != Math.Max(1, config.lengthUnits / config.capacity))
                throw new ArgumentException($"Conveyor {config.nodeId} has invalid length, speed, capacity, or spacing.");
        }

        protected override IFactoryNode Create(ConveyorNodeConfig config) => new ConveyorNode(config);
    }
}
