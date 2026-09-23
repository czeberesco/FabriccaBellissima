using System;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    public sealed class FactorySimulationCoordinator : MonoBehaviour
    {
        [SerializeField] private FactoryDemoAuthoring _authoring;
        [SerializeField] private bool _playOnStart = true;
        [SerializeField, Range(0.1f, 20f)] private float _simulationSpeed = 1f;
        [SerializeField, Min(1)] private int _maximumTicksPerFrame = 200;
        [SerializeField] private bool _showControls = true;

        private FactorySimulation _simulation;
        private float _accumulator;
        private Vector2 _scrollPosition;
        private string _initializationError = string.Empty;

        public FactorySimulation Simulation => _simulation;
        public bool IsPlaying { get; private set; }
        public float SimulationSpeed => _simulationSpeed;
        public FactorySnapshot CurrentSnapshot => _simulation?.CurrentSnapshot;

        private void Awake()
        {
            if (_authoring == null)
            {
                _authoring = GetComponent<FactoryDemoAuthoring>();
            }

            try
            {
                if (_authoring == null)
                    throw new InvalidOperationException("FactoryDemoAuthoring is required beside the coordinator.");
                Initialize(_authoring.CompileScenario());
                IsPlaying = _playOnStart;
            }
            catch (Exception exception)
            {
                _initializationError = exception.Message;
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!IsPlaying || _simulation == null)
            {
                return;
            }

            _accumulator += Time.unscaledDeltaTime * _simulationSpeed;
            int executed = 0;
            while (_accumulator >= _simulation.TickDurationSeconds && executed < _maximumTicksPerFrame)
            {
                StepOneTick();
                _accumulator -= _simulation.TickDurationSeconds;
                executed++;
            }
        }

        public void Initialize(FactoryScenario scenario)
        {
            _simulation = new FactorySimulation(scenario);
            _accumulator = 0f;
            _initializationError = string.Empty;
        }

        public void StepOneTick()
        {
            if (_simulation == null) throw new InvalidOperationException("Simulation has not been initialized.");
            _simulation.StepOneTick();
        }

        public void StepTicks(int count)
        {
            if (_simulation == null) throw new InvalidOperationException("Simulation has not been initialized.");
            _simulation.StepTicks(count);
        }

        public void Play() => IsPlaying = true;
        public void Pause() => IsPlaying = false;

        public void ResetSimulation()
        {
            if (_simulation == null) return;
            _simulation.ResetSimulation();
            _accumulator = 0f;
            IsPlaying = false;
        }

        public void SetSimulationSpeed(float speed) => _simulationSpeed = Mathf.Clamp(speed, 0.1f, 20f);
        public void SetNodeEnabled(int nodeId, bool enabled) => _simulation?.SetNodeEnabled(nodeId, enabled);
        public string ExportCanonicalSnapshot(bool pretty = true) =>
            _simulation == null ? string.Empty : _simulation.ExportCanonicalSnapshot(pretty);

        private void OnGUI()
        {
            if (!_showControls)
            {
                return;
            }

            float panelWidth = Mathf.Min(390f, Mathf.Max(280f, Screen.width * 0.3f - 18f));
            GUI.Box(new Rect(12f, 12f, panelWidth, Screen.height - 24f), GUIContent.none);
            GUILayout.BeginArea(new Rect(24f, 20f, panelWidth - 24f, Screen.height - 40f));
            GUILayout.Label("FABBRICA BELLISSIMA — deterministic factory");
            if (!string.IsNullOrEmpty(_initializationError))
            {
                GUILayout.Label($"ERROR: {_initializationError}");
                GUILayout.EndArea();
                return;
            }

            FactorySnapshot snapshot = CurrentSnapshot;
            GUILayout.Label($"Tick {snapshot.tick}   Time {snapshot.tick * _simulation.TickDurationSeconds:0.00}s   " +
                            (IsPlaying ? "PLAYING" : "PAUSED"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(IsPlaying ? "Pause" : "Play"))
            {
                if (IsPlaying) Pause(); else Play();
            }
            if (GUILayout.Button("Single Tick")) { Pause(); StepOneTick(); }
            if (GUILayout.Button("Reset")) ResetSimulation();
            GUILayout.EndHorizontal();
            GUILayout.Label($"Simulation speed: {_simulationSpeed:0.0}x (belt speed unchanged)");
            _simulationSpeed = GUILayout.HorizontalSlider(_simulationSpeed, 0.1f, 10f);
            GUILayout.Label($"Made: ore {snapshot.counters.ironOreGenerated}, coal {snapshot.counters.coalGenerated}, " +
                            $"cans {snapshot.counters.paintCansGenerated}, bars {snapshot.counters.ironBarsMade}, " +
                            $"painted {snapshot.counters.paintedBarsMade}, consumed {snapshot.counters.sinkConsumed}");
            GUILayout.Label($"Accounting: coal burned {snapshot.counters.coalConsumed}, cans opened " +
                            $"{snapshot.counters.cansOpened}, paint charges applied {snapshot.counters.chargesApplied}");

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            foreach (FactoryNodeSnapshot node in snapshot.nodes)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{node.nodeId}  {node.displayName}");
                bool enabledState = GUILayout.Toggle(node.enabled, "Enabled", GUILayout.Width(80f));
                if (enabledState != node.enabled) SetNodeEnabled(node.nodeId, enabledState);
                GUILayout.EndHorizontal();
                GUILayout.Label(BuildNodeStatus(node));
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string BuildNodeStatus(FactoryNodeSnapshot node)
        {
            switch (node.kind)
            {
                case FactoryNodeKind.Extractor:
                    return $"{node.status} | work {node.workProgress}/{node.workRequired} | " +
                           $"output {(node.outputItem == null ? "empty" : node.outputItem.resource + " #" + node.outputItem.id)}";
                case FactoryNodeKind.Conveyor:
                    return $"{node.status} | capacity {node.beltItems.Count}/{node.capacity}";
                case FactoryNodeKind.Furnace:
                    return $"{node.status} | smelt {node.workProgress}/{node.workRequired} | fuel {node.fuelRemaining} | " +
                           $"queued coal {(node.waitingCoal == null ? "no" : "#" + node.waitingCoal.id)}";
                case FactoryNodeKind.ColoringStation:
                    return $"{node.status} | bar {node.appliedCharges}/{node.chargesPerBar} | spray " +
                           $"{node.sprayProgress}/{node.sprayTicks} | reservoir {node.reservoirColorId} " +
                           $"{node.reservoirCharges} | " +
                           $"waiting can {(node.waitingCan == null ? "no" : "#" + node.waitingCan.id)}";
                case FactoryNodeKind.Sink:
                    return $"{node.status} | last ID " +
                           (node.consumedItemIds.Count == 0 ? "none" : node.consumedItemIds[node.consumedItemIds.Count - 1].ToString());
                default:
                    return node.status;
            }
        }
    }
}
