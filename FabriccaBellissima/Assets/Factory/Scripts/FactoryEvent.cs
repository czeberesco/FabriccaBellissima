using System;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryEvent
    {
        public long tick;
        public FactoryEventKind kind;
        public int nodeId;
        public int portId;
        public int otherNodeId;
        public int otherPortId;
        public long itemId;
        public FactoryResourceType resourceBefore;
        public FactoryResourceType resourceAfter;
        public string colorId = string.Empty;
        public int amount;
    }
}
