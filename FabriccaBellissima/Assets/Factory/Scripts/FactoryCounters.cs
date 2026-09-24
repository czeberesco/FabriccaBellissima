using System;

namespace FabriccaBellissima.Factory
{
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

        public FactoryCounters Clone() => (FactoryCounters)MemberwiseClone();
    }
}
