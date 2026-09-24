namespace FabriccaBellissima.Factory
{
    public interface IFactoryNodeFactory
    {
        FactoryNodeKind Kind { get; }
        void Validate(FactoryNodeConfig config);
        IFactoryNode Create(FactoryNodeConfig config);
    }
}
