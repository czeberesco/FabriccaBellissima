using System;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryNodeConfig
    {
        public int nodeId;
        public string displayName = string.Empty;
        public FactoryNodeKind kind;
        public bool enabled = true;
        public FactoryResourceType resource;
        public string colorId = "Blue";
        public int durationTicks;
        public int paintCanCharges;
        public int lengthUnits;
        public int movementPerTick;
        public int capacity;
        public int spacingUnits;
        public int burnTicks;
        public int smeltTicks;
        public int sprayTicks;
        public int chargesPerBar;
        public Vector3 position;
        public Vector3 beltStart;
        public Vector3 beltEnd;
    }
}
