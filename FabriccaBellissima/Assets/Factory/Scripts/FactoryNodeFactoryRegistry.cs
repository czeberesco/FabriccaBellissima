using System;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryNodeFactoryRegistry
    {
        private readonly Dictionary<FactoryNodeKind, IFactoryNodeFactory> _factories =
            new Dictionary<FactoryNodeKind, IFactoryNodeFactory>();

        public FactoryNodeFactoryRegistry(IEnumerable<IFactoryNodeFactory> factories)
        {
            if (factories == null) throw new ArgumentNullException(nameof(factories));
            foreach (IFactoryNodeFactory factory in factories)
            {
                if (factory == null || !_factories.TryAdd(factory.Kind, factory))
                    throw new ArgumentException("Node factories must be non-null and unique by node kind.", nameof(factories));
            }
        }

        public IFactoryNode Create(FactoryNodeConfig config) => Get(config.kind).Create(config);
        public void Validate(FactoryNodeConfig config) => Get(config.kind).Validate(config);

        private IFactoryNodeFactory Get(FactoryNodeKind kind)
        {
            if (!_factories.TryGetValue(kind, out IFactoryNodeFactory factory))
                throw new ArgumentException($"No node factory is registered for {kind}.");
            return factory;
        }

        public static FactoryNodeFactoryRegistry CreateDefault() => new FactoryNodeFactoryRegistry(new IFactoryNodeFactory[]
        {
            new ExtractorNodeFactory(),
            new ConveyorNodeFactory(),
            new FurnaceNodeFactory(),
            new ColoringStationNodeFactory(),
            new SinkNodeFactory()
        });
    }
}
