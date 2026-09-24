using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class SinkNodeConfig : FactoryNodeConfig
    {
        public string acceptedColorId = "Blue";

        public SinkNodeConfig() : base(FactoryNodeKind.Sink) { }
    }
}
