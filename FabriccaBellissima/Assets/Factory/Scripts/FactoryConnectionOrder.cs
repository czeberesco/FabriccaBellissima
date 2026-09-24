namespace FabriccaBellissima.Factory
{
    public static class FactoryConnectionOrder
    {
        public static int Compare(FactoryConnection left, FactoryConnection right)
        {
            int comparison = left.sourceNodeId.CompareTo(right.sourceNodeId);
            if (comparison != 0) return comparison;
            comparison = left.sourcePortId.CompareTo(right.sourcePortId);
            if (comparison != 0) return comparison;
            comparison = left.destinationNodeId.CompareTo(right.destinationNodeId);
            return comparison != 0 ? comparison : left.destinationPortId.CompareTo(right.destinationPortId);
        }
    }
}
