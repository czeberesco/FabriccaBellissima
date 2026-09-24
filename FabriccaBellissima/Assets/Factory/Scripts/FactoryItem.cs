using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryItem
    {
        public long id;
        public FactoryResourceType resource;
        public string colorId = string.Empty;
        public int charges;

        public FactoryItem Clone() => new FactoryItem
        {
            id = id,
            resource = resource,
            colorId = colorId,
            charges = charges
        };
    }
}
