using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryConnection
    {
        public int sourceNodeId;
        public int sourcePortId;
        public int destinationNodeId;
        public int destinationPortId;
    }
}
