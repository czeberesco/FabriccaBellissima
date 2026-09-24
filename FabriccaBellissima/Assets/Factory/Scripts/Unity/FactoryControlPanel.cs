using System;
using UnityEngine;
using Zenject;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryControlPanel : MonoBehaviour
    {
        [SerializeField] private bool _visible = true;
        private IFactorySimulationController _controller;
        private Vector2 _scrollPosition;

        [Inject]
        public void Construct(IFactorySimulationController controller) =>
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

        private void OnGUI()
        {
            if (!_visible || _controller == null) return;
            FactorySnapshot snapshot = _controller.CurrentSnapshot;
            if (snapshot == null) return;

            float panelWidth = Mathf.Min(390f, Mathf.Max(280f, Screen.width * 0.3f - 18f));
            GUI.Box(new Rect(12f, 12f, panelWidth, Screen.height - 24f), GUIContent.none);
            GUILayout.BeginArea(new Rect(24f, 20f, panelWidth - 24f, Screen.height - 40f));
            GUILayout.Label("FABBRICA BELLISSIMA — deterministic factory");
            GUILayout.Label($"Tick {snapshot.tick}   " + (_controller.IsPlaying ? "PLAYING" : "PAUSED"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_controller.IsPlaying ? "Pause" : "Play"))
            {
                if (_controller.IsPlaying) _controller.Pause(); else _controller.Play();
            }
            if (GUILayout.Button("Single Tick")) { _controller.Pause(); _controller.StepOneTick(); }
            if (GUILayout.Button("Reset")) _controller.ResetSimulation();
            GUILayout.EndHorizontal();
            GUILayout.Label($"Simulation speed: {_controller.SimulationSpeed:0.0}x (belt speed unchanged)");
            _controller.SetSimulationSpeed(GUILayout.HorizontalSlider(_controller.SimulationSpeed, 0.1f, 10f));
            GUILayout.Label($"Made: ore {snapshot.counters.ironOreGenerated}, coal {snapshot.counters.coalGenerated}, " +
                            $"cans {snapshot.counters.paintCansGenerated}, bars {snapshot.counters.ironBarsMade}, " +
                            $"painted {snapshot.counters.paintedBarsMade}, consumed {snapshot.counters.sinkConsumed}");
            GUILayout.Label($"Accounting: coal burned {snapshot.counters.coalConsumed}, cans opened " +
                            $"{snapshot.counters.cansOpened}, paint charges applied {snapshot.counters.chargesApplied}");

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            for (int index = 0; index < snapshot.nodes.Count; index++)
            {
                FactoryNodeSnapshot node = snapshot.nodes[index];
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{node.nodeId}  {node.displayName}");
                bool enabledState = GUILayout.Toggle(node.enabled, "Enabled", GUILayout.Width(80f));
                if (enabledState != node.enabled) _controller.SetNodeEnabled(node.nodeId, enabledState);
                GUILayout.EndHorizontal();
                GUILayout.Label(FactoryNodeStatusText.Format(node));
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
