using System;
using System.Collections.Generic;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    public enum FactoryResourceType
    {
        None,
        IronOre,
        Coal,
        PaintCan,
        IronBar,
        PaintedIronBar
    }

    public enum FactoryNodeKind
    {
        Extractor,
        Conveyor,
        Furnace,
        ColoringStation,
        Sink
    }

    public enum FactoryEventKind
    {
        Generated,
        CoalIgnited,
        CanOpened,
        ChargeApplied,
        Transformed,
        Transferred,
        SinkConsumed
    }

    public static class FactoryPorts
    {
        public const int Input = 10;
        public const int Output = 20;
        public const int FurnaceOre = 10;
        public const int FurnaceFuel = 20;
        public const int FurnaceOutput = 30;
        public const int ColoringBar = 10;
        public const int ColoringCan = 20;
        public const int ColoringOutput = 30;
    }

    [Serializable]
    public sealed class FactoryItem
    {
        public long id;
        public FactoryResourceType resource;
        public string colorId = string.Empty;
        public int charges;

        public FactoryItem Clone()
        {
            return new FactoryItem { id = id, resource = resource, colorId = colorId, charges = charges };
        }
    }

    [Serializable]
    public sealed class FactoryBeltItem
    {
        public FactoryItem item = new FactoryItem();
        public int progressUnits;

        public FactoryBeltItem Clone()
        {
            return new FactoryBeltItem { item = item.Clone(), progressUnits = progressUnits };
        }
    }

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

    [Serializable]
    public sealed class FactoryConnection
    {
        public int sourceNodeId;
        public int sourcePortId;
        public int destinationNodeId;
        public int destinationPortId;
    }

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

    [Serializable]
    public sealed class FactoryScenario
    {
        public int formatVersion = 1;
        public int tickDurationMicroseconds = 50000;
        public long initialTick;
        public long nextItemId = 1;
        public List<FactoryNodeConfig> nodes = new List<FactoryNodeConfig>();
        public List<FactoryConnection> connections = new List<FactoryConnection>();
        public List<FactoryNodeInitialState> initialStates = new List<FactoryNodeInitialState>();

        public string ToJson(bool pretty = true)
        {
            // Clone before sorting so export never mutates the live Inspector/scenario order.
            FactoryScenario canonical = JsonUtility.FromJson<FactoryScenario>(JsonUtility.ToJson(this));
            canonical.nodes.Sort((left, right) => left.nodeId.CompareTo(right.nodeId));
            canonical.connections.Sort((left, right) =>
            {
                int comparison = left.sourceNodeId.CompareTo(right.sourceNodeId);
                if (comparison != 0) return comparison;
                comparison = left.sourcePortId.CompareTo(right.sourcePortId);
                if (comparison != 0) return comparison;
                comparison = left.destinationNodeId.CompareTo(right.destinationNodeId);
                return comparison != 0 ? comparison : left.destinationPortId.CompareTo(right.destinationPortId);
            });
            canonical.initialStates.Sort((left, right) => left.nodeId.CompareTo(right.nodeId));
            return JsonUtility.ToJson(canonical, pretty);
        }

        public static FactoryScenario FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Scenario JSON cannot be empty.", nameof(json));
            }

            return JsonUtility.FromJson<FactoryScenario>(json);
        }
    }

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

    [Serializable]
    public sealed class FactoryCounters
    {
        public int ironOreGenerated;
        public int coalGenerated;
        public int paintCansGenerated;
        public int coalConsumed;
        public int cansOpened;
        public int chargesApplied;
        public int ironBarsMade;
        public int paintedBarsMade;
        public int sinkConsumed;

        public FactoryCounters Clone()
        {
            return (FactoryCounters)MemberwiseClone();
        }
    }

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

    public interface IFactorySimulation
    {
        long Tick { get; }
        float TickDurationSeconds { get; }
        FactorySnapshot CurrentSnapshot { get; }
        void StepOneTick();
        void StepTicks(int count);
        void ResetSimulation();
        string ExportCanonicalSnapshot(bool pretty = false);
    }
}
