using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class ConveyorNodeConfig : FactoryNodeConfig
    {
        public int lengthUnits;
        public int movementPerTick;
        public int capacity;
        public int spacingUnits;

        public ConveyorNodeConfig() : base(FactoryNodeKind.Conveyor) { }
    }
}
