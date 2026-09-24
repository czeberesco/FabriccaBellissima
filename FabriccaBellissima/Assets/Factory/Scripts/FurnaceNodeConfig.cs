using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FurnaceNodeConfig : FactoryNodeConfig
    {
        public int burnTicks;
        public int smeltTicks;

        public FurnaceNodeConfig() : base(FactoryNodeKind.Furnace) { }
    }
}
