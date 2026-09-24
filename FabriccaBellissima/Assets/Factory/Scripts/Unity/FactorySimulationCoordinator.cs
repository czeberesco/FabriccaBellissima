using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using Zenject;

namespace FabriccaBellissima.Factory
{
    public sealed class FactorySimulationCoordinator : MonoBehaviour, IFactorySimulationController,
        IInitializable, IDisposable
    {
        [SerializeField] private bool _playOnStart = true;
        [SerializeField, Range(0.1f, 20f)] private float _simulationSpeed = 1f;
        [SerializeField, Min(1)] private int _maximumTicksPerFrame = 200;

        private readonly Subject<FactorySnapshot> _snapshots = new Subject<FactorySnapshot>();
        private FactoryDemoAuthoring _authoring;
        private IFactorySimulationFactory _simulationFactory;
        private IDisposable _frameSubscription;
        private FactorySimulation _simulation;
        private float _accumulator;
        private string _initializationError = string.Empty;

        public FactorySimulation Simulation => _simulation;
        public bool IsPlaying { get; private set; }
        public float SimulationSpeed => _simulationSpeed;
        public FactorySnapshot CurrentSnapshot => _simulation?.CurrentSnapshot;
        public IReadOnlyList<FactoryNodeConfig> NodeConfigs => _simulation?.NodeConfigs;
        public IReadOnlyList<FactoryNodeLayout> NodeLayouts => _authoring?.NodeLayouts;
        public Observable<FactorySnapshot> Snapshots => _snapshots;
        public string InitializationError => _initializationError;

        [Inject]
        public void Construct(FactoryDemoAuthoring authoring, IFactorySimulationFactory simulationFactory)
        {
            _authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
            _simulationFactory = simulationFactory ?? throw new ArgumentNullException(nameof(simulationFactory));
        }

        public void Initialize()
        {
            if (_simulation == null)
            {
                try
                {
                    if (_authoring == null || _simulationFactory == null)
                        throw new InvalidOperationException("Factory scene dependencies were not injected.");
                    Initialize(_authoring.CompileScenario());
                    IsPlaying = _playOnStart;
                }
                catch (Exception exception)
                {
                    _initializationError = exception.Message;
                    Debug.LogException(exception, this);
                    enabled = false;
                    return;
                }
            }

            _frameSubscription = Observable.EveryUpdate().Subscribe(_ => AdvanceFromPlayerLoop());
        }

        public void Dispose()
        {
            _frameSubscription?.Dispose();
            _snapshots.Dispose();
        }

        public void Initialize(FactoryScenario scenario)
        {
            if (_simulationFactory == null)
                throw new InvalidOperationException("Factory scene dependencies were not injected.");
            _simulation = _simulationFactory.Create(scenario);
            _accumulator = 0f;
            _initializationError = string.Empty;
            PublishSnapshot();
        }

        public void StepOneTick()
        {
            EnsureInitialized();
            _simulation.StepOneTick();
            PublishSnapshot();
        }

        public void StepTicks(int count)
        {
            EnsureInitialized();
            _simulation.StepTicks(count);
            PublishSnapshot();
        }

        public void Play() => IsPlaying = true;
        public void Pause() => IsPlaying = false;

        public void ResetSimulation()
        {
            if (_simulation == null) return;
            _simulation.ResetSimulation();
            _accumulator = 0f;
            IsPlaying = false;
            PublishSnapshot();
        }

        public void SetSimulationSpeed(float speed) => _simulationSpeed = Mathf.Clamp(speed, 0.1f, 20f);

        public void SetNodeEnabled(int nodeId, bool enabled)
        {
            if (_simulation == null) return;
            _simulation.SetNodeEnabled(nodeId, enabled);
            PublishSnapshot();
        }

        public string ExportCanonicalSnapshot(bool pretty = true) =>
            _simulation == null ? string.Empty : _simulation.ExportCanonicalSnapshot(pretty);

        private void AdvanceFromPlayerLoop()
        {
            if (!IsPlaying || _simulation == null) return;
            _accumulator += Time.unscaledDeltaTime * _simulationSpeed;
            int executed = 0;
            while (_accumulator >= _simulation.TickDurationSeconds && executed < _maximumTicksPerFrame)
            {
                _simulation.StepOneTick();
                _accumulator -= _simulation.TickDurationSeconds;
                executed++;
            }
            if (executed > 0) PublishSnapshot();
        }

        private void PublishSnapshot()
        {
            if (_simulation != null) _snapshots.OnNext(_simulation.CurrentSnapshot);
        }

        private void EnsureInitialized()
        {
            if (_simulation == null) throw new InvalidOperationException("Simulation has not been initialized.");
        }
    }
}
