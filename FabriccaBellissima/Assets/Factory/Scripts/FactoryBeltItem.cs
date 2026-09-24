using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryBeltItem
    {
        public FactoryItem item = new FactoryItem();
        public int progressUnits;

        public FactoryBeltItem Clone() => new FactoryBeltItem
        {
            item = item.Clone(),
            progressUnits = progressUnits
        };
    }
}
