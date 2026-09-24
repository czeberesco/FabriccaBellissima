using UnityEngine;
using Zenject;
using System;

namespace FabriccaBellissima.Factory
{
    public sealed class FactorySceneInstaller : MonoInstaller
    {
        [SerializeField] private FactoryDemoAuthoring _authoring;
        [SerializeField] private FactorySimulationCoordinator _coordinator;
        [SerializeField] private FactoryPresentation _presentation;
        [SerializeField] private FactoryControlPanel _controlPanel;

        public void Configure(FactoryDemoAuthoring authoring, FactorySimulationCoordinator coordinator,
            FactoryPresentation presentation, FactoryControlPanel controlPanel)
        {
            _authoring = authoring;
            _coordinator = coordinator;
            _presentation = presentation;
            _controlPanel = controlPanel;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(_authoring).AsSingle();
            Container.BindInstance(_coordinator).AsSingle();
            Container.Bind<IFactorySimulationController>().FromInstance(_coordinator).AsSingle();
            Container.Bind<IInitializable>().FromInstance(_coordinator).AsSingle();
            Container.Bind<IDisposable>().FromInstance(_coordinator).AsSingle();
            Container.BindInstance(_presentation).AsSingle();
            Container.BindInstance(_controlPanel).AsSingle();
        }
    }
}
