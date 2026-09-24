using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class ExtractorNodeConfig : FactoryNodeConfig
    {
        public FactoryResourceType resource;
        public int durationTicks;
        public string colorId = string.Empty;
        public int paintCanCharges;

        public ExtractorNodeConfig() : base(FactoryNodeKind.Extractor) { }
    }
}
