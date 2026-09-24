using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryTickContext
    {
        private readonly List<FactoryEvent> _events;

        public FactoryTickContext(long nextItemId, FactoryCounters counters, List<FactoryEvent> events)
        {
            NextItemId = nextItemId;
            Counters = counters;
            _events = events;
        }

        public long Tick { get; private set; }
        public long NextItemId { get; private set; }
        public FactoryCounters Counters { get; }

        public void BeginTick(long tick) => Tick = tick;
        public long AllocateItemId() => NextItemId++;

        public void AddEvent(FactoryEventKind kind, int nodeId, int portId, FactoryItem item,
            FactoryResourceType before, FactoryResourceType after, int otherNodeId = 0,
            int otherPortId = 0, int amount = 0)
        {
            _events.Add(new FactoryEvent
            {
                tick = Tick,
                kind = kind,
                nodeId = nodeId,
                portId = portId,
                otherNodeId = otherNodeId,
                otherPortId = otherPortId,
                itemId = item?.id ?? 0,
                resourceBefore = before,
                resourceAfter = after,
                colorId = item?.colorId ?? string.Empty,
                amount = amount
            });
        }

        public void IncrementGenerated(FactoryResourceType resource)
        {
            if (resource == FactoryResourceType.IronOre) Counters.ironOreGenerated++;
            else if (resource == FactoryResourceType.Coal) Counters.coalGenerated++;
            else if (resource == FactoryResourceType.PaintCan) Counters.paintCansGenerated++;
        }
    }
}
