using System;
using System.Collections.Generic;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    [Serializable]
    public sealed class FactoryScenario
    {
        public int formatVersion = 1;
        public int tickDurationMicroseconds = 50000;
        public long initialTick;
        public long nextItemId = 1;
        [SerializeReference] public List<FactoryNodeConfig> nodes = new List<FactoryNodeConfig>();
        public List<FactoryConnection> connections = new List<FactoryConnection>();
        public List<FactoryNodeInitialState> initialStates = new List<FactoryNodeInitialState>();

        public string ToJson(bool pretty = true)
        {
            FactoryScenario canonical = JsonUtility.FromJson<FactoryScenario>(JsonUtility.ToJson(this));
            canonical.nodes.Sort((left, right) => left.nodeId.CompareTo(right.nodeId));
            canonical.connections.Sort(FactoryConnectionOrder.Compare);
            canonical.initialStates.Sort((left, right) => left.nodeId.CompareTo(right.nodeId));
            return JsonUtility.ToJson(canonical, pretty);
        }

        public static FactoryScenario FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Scenario JSON cannot be empty.", nameof(json));
            return JsonUtility.FromJson<FactoryScenario>(json);
        }
    }
}
