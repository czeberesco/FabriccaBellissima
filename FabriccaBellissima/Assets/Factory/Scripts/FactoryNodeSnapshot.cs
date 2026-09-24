using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryNodeSnapshot
    {
        public int nodeId;
        public string displayName = string.Empty;
        public FactoryNodeKind kind;
        public bool enabled;
        public string status = string.Empty;
        public FactoryResourceType configuredResource;
        public string configuredColorId = string.Empty;
        public int durationTicks;
        public int paintCanCharges;
        public int workProgress;
        public int workRequired;
        public int fuelRemaining;
        public int burnTicks;
        public int reservoirCharges;
        public string reservoirColorId = string.Empty;
        public int appliedCharges;
        public int chargesPerBar;
        public int sprayProgress;
        public int sprayTicks;
        public int lengthUnits;
        public int movementPerTick;
        public int capacity;
        public int spacingUnits;
        public FactoryItem outputItem;
        public FactoryItem workItem;
        public FactoryItem waitingCoal;
        public FactoryItem waitingCan;
        public List<FactoryBeltItem> beltItems = new List<FactoryBeltItem>();
        public int sinkTotal;
        public List<string> sinkColors = new List<string>();
        public List<int> sinkColorCounts = new List<int>();
        public List<long> consumedItemIds = new List<long>();
    }
}
