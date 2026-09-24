using System;
using System.Collections.Generic;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactorySnapshot
    {
        public int formatVersion = 1;
        public long tick;
        public int tickDurationMicroseconds;
        public long nextItemId;
        public FactoryCounters counters = new FactoryCounters();
        public List<FactoryNodeSnapshot> nodes = new List<FactoryNodeSnapshot>();
        public List<FactoryConnection> connections = new List<FactoryConnection>();
        public List<FactoryEvent> events = new List<FactoryEvent>();

        public string ToCanonicalJson(bool pretty = false) => JsonUtility.ToJson(this, pretty);
    }
}
