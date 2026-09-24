using Zenject;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IFactoryNodeFactory>().To<ExtractorNodeFactory>().AsSingle();
            Container.Bind<IFactoryNodeFactory>().To<ConveyorNodeFactory>().AsSingle();
            Container.Bind<IFactoryNodeFactory>().To<FurnaceNodeFactory>().AsSingle();
            Container.Bind<IFactoryNodeFactory>().To<ColoringStationNodeFactory>().AsSingle();
            Container.Bind<IFactoryNodeFactory>().To<SinkNodeFactory>().AsSingle();
            Container.Bind<FactoryNodeFactoryRegistry>().AsSingle();
            Container.Bind<FactoryTransferResolver>().AsSingle();
            Container.Bind<FactorySnapshotBuilder>().AsSingle();
            Container.Bind<IFactorySimulationFactory>().To<FactorySimulationFactory>().AsSingle();
        }
    }
}
