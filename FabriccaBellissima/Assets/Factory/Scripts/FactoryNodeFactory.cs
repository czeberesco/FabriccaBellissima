using System;

namespace FabriccaBellissima.Factory
{
    public abstract class FactoryNodeFactory<TConfig> : IFactoryNodeFactory
        where TConfig : FactoryNodeConfig
    {
        public abstract FactoryNodeKind Kind { get; }

        public void Validate(FactoryNodeConfig config)
        {
            ValidateType(config);
            Validate((TConfig)config);
        }

        public IFactoryNode Create(FactoryNodeConfig config)
        {
            ValidateType(config);
            return Create((TConfig)config);
        }

        protected abstract void Validate(TConfig config);
        protected abstract IFactoryNode Create(TConfig config);

        private void ValidateType(FactoryNodeConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.kind != Kind)
                throw new ArgumentException($"Node {config.nodeId} declares kind {config.kind}, but factory {Kind} was selected.");
            if (!(config is TConfig))
                throw new ArgumentException(
                    $"Node {config.nodeId} of kind {Kind} requires {typeof(TConfig).Name}, not {config.GetType().Name}.");
        }
    }
}
