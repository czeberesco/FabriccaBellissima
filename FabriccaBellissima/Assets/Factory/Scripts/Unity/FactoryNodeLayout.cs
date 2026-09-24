using System;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryNodeLayout
    {
        public int nodeId;
        public Vector3 position;
        public Vector3 beltStart;
        public Vector3 beltEnd;
    }
}
