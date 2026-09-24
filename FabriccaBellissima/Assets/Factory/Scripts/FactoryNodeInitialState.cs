using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryNodeInitialState
    {
        public int nodeId;
        public int workProgress;
        public int fuelRemaining;
        public int appliedCharges;
        public int sprayProgress;
        public int reservoirCharges;
        public string reservoirColorId = string.Empty;
        public FactoryItem outputItem;
        public FactoryItem workItem;
        public FactoryItem waitingCoal;
        public FactoryItem waitingCan;
        public List<FactoryBeltItem> beltItems = new List<FactoryBeltItem>();
    }
}
