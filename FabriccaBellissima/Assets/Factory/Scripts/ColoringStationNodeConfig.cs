using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class ColoringStationNodeConfig : FactoryNodeConfig
    {
        public string colorId = "Blue";
        public int sprayTicks;
        public int chargesPerBar;

        public ColoringStationNodeConfig() : base(FactoryNodeKind.ColoringStation) { }
    }
}
