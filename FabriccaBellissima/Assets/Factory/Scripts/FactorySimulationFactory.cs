using System;

namespace FabriccaBellissima.Factory
{
    public sealed class FactorySimulationFactory : IFactorySimulationFactory
    {
        private readonly FactoryNodeFactoryRegistry _nodeFactories;
        private readonly FactoryTransferResolver _transferResolver;
        private readonly FactorySnapshotBuilder _snapshotBuilder;

        public FactorySimulationFactory(FactoryNodeFactoryRegistry nodeFactories,
            FactoryTransferResolver transferResolver, FactorySnapshotBuilder snapshotBuilder)
        {
            _nodeFactories = nodeFactories ?? throw new ArgumentNullException(nameof(nodeFactories));
            _transferResolver = transferResolver ?? throw new ArgumentNullException(nameof(transferResolver));
            _snapshotBuilder = snapshotBuilder ?? throw new ArgumentNullException(nameof(snapshotBuilder));
        }

        public FactorySimulation Create(FactoryScenario scenario) =>
            new FactorySimulation(scenario, _nodeFactories, _transferResolver, _snapshotBuilder);
    }
}
