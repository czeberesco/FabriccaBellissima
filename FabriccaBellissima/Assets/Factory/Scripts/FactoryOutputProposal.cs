namespace FabriccaBellissima.Factory
{
    public readonly struct FactoryOutputProposal
    {
        public FactoryOutputProposal(IFactoryNode source, int sourcePortId, FactoryItem item)
        {
            Source = source;
            SourcePortId = sourcePortId;
            Item = item;
        }

        public IFactoryNode Source { get; }
        public int SourcePortId { get; }
        public FactoryItem Item { get; }
    }
}
