using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace FabriccaBellissima.Factory.Tests
{
    public sealed class FactoryDeterminismRuntimeTests
    {
        private const int TicksPerRun = 10000;
        private const int RunsPerScenario = 100;
        private const int FullStateCheckpointInterval = 250;

        [Test]
        public void FiveScheduledScenariosRemainExactlyDeterministicAcrossOneHundredRuns()
        {
            FactoryScenario factoryScenario = CreateDefaultScenario();
            ScenarioDefinition[] scenarios = CreateScenarios();
            var totalStopwatch = Stopwatch.StartNew();

            foreach (ScenarioDefinition scenario in scenarios)
            {
                var scenarioStopwatch = Stopwatch.StartNew();
                RunResult expected = Execute(factoryScenario, scenario);

                for (int run = 2; run <= RunsPerScenario; run++)
                {
                    RunResult actual = Execute(factoryScenario, scenario);
                    Assert.That(actual.FinalCanonicalState, Is.EqualTo(expected.FinalCanonicalState),
                        $"Final logical state diverged in '{scenario.Name}' on run {run}.");
                    Assert.That(actual.TraceHash, Is.EqualTo(expected.TraceHash),
                        $"Event/checkpoint trace diverged in '{scenario.Name}' on run {run}.");
                }

                scenarioStopwatch.Stop();
                TestContext.Out.WriteLine(
                    $"{scenario.Name}: {RunsPerScenario:N0} x {TicksPerRun:N0} ticks passed " +
                    $"in {scenarioStopwatch.Elapsed.TotalSeconds:0.000}s; SHA-256 {expected.TraceHash}");
            }

            totalStopwatch.Stop();
            TestContext.Out.WriteLine(
                $"Determinism soak passed: {scenarios.Length * RunsPerScenario * TicksPerRun:N0} total ticks " +
                $"in {totalStopwatch.Elapsed.TotalSeconds:0.000}s.");
        }

        private static RunResult Execute(FactoryScenario factoryScenario, ScenarioDefinition definition)
        {
            var simulation = new FactorySimulation(factoryScenario);
            using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                int nextToggle = 0;
                for (int tick = 1; tick <= TicksPerRun; tick++)
                {
                    while (nextToggle < definition.Toggles.Length &&
                           definition.Toggles[nextToggle].Tick == tick)
                    {
                        ScheduledToggle toggle = definition.Toggles[nextToggle++];
                        simulation.SetNodeEnabled(toggle.NodeId, toggle.Enabled);
                        Append(hash, $"T|{tick}|{toggle.NodeId}|{(toggle.Enabled ? 1 : 0)}\n");
                    }

                    simulation.StepOneTick();
                    AppendEvents(hash, simulation.LastEvents);

                    if (tick % FullStateCheckpointInterval == 0)
                    {
                        Append(hash, $"S|{tick}|");
                        Append(hash, simulation.ExportCanonicalSnapshot());
                        Append(hash, "\n");
                    }
                }

                Assert.That(nextToggle, Is.EqualTo(definition.Toggles.Length),
                    $"Scenario '{definition.Name}' contains an unreachable or unsorted toggle.");
                string finalState = simulation.ExportCanonicalSnapshot();
                Append(hash, "F|");
                Append(hash, finalState);
                string traceHash = BitConverter.ToString(hash.GetHashAndReset()).Replace("-", string.Empty);
                return new RunResult(finalState, traceHash);
            }
        }

        private static void AppendEvents(IncrementalHash hash, IReadOnlyList<FactoryEvent> events)
        {
            foreach (FactoryEvent factoryEvent in events)
            {
                Append(hash, $"E|{factoryEvent.tick}|{(int)factoryEvent.kind}|{factoryEvent.nodeId}|" +
                             $"{factoryEvent.portId}|{factoryEvent.otherNodeId}|{factoryEvent.otherPortId}|" +
                             $"{factoryEvent.itemId}|{(int)factoryEvent.resourceBefore}|" +
                             $"{(int)factoryEvent.resourceAfter}|{factoryEvent.colorId}|{factoryEvent.amount}\n");
            }
        }

        private static void Append(IncrementalHash hash, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            hash.AppendData(bytes);
        }

        private static FactoryScenario CreateDefaultScenario()
        {
            var gameObject = new GameObject("Determinism scenario authoring");
            try
            {
                FactoryDemoAuthoring authoring = gameObject.AddComponent<FactoryDemoAuthoring>();
                authoring.ResetToDefaults();
                return authoring.CompileScenario();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static ScenarioDefinition[] CreateScenarios()
        {
            return new[]
            {
                new ScenarioDefinition("01 - uninterrupted baseline"),
                new ScenarioDefinition("02 - core machines interrupted",
                    Toggle(10, 400, false), Toggle(10, 900, true),
                    Toggle(50, 2200, false), Toggle(50, 2900, true),
                    Toggle(90, 5100, false), Toggle(90, 5900, true)),
                new ScenarioDefinition("03 - supply and output interruption",
                    Toggle(30, 600, false), Toggle(30, 1300, true),
                    Toggle(40, 1800, false), Toggle(40, 2400, true),
                    Toggle(70, 3600, false), Toggle(70, 4700, true),
                    Toggle(100, 7200, false), Toggle(100, 8100, true)),
                new ScenarioDefinition("04 - transport and sink congestion",
                    Toggle(20, 500, false), Toggle(20, 1050, true),
                    Toggle(50, 1700, false), Toggle(50, 2300, true),
                    Toggle(60, 3100, false), Toggle(60, 3900, true),
                    Toggle(80, 4800, false), Toggle(80, 5650, true),
                    Toggle(110, 7000, false), Toggle(110, 8350, true)),
                new ScenarioDefinition("05 - overlapping six-node outage",
                    Toggle(10, 300, false), Toggle(30, 450, false),
                    Toggle(40, 800, false), Toggle(10, 1200, true),
                    Toggle(70, 1500, false), Toggle(30, 1750, true),
                    Toggle(40, 2100, true), Toggle(90, 2600, false),
                    Toggle(70, 3300, true), Toggle(100, 4100, false),
                    Toggle(90, 5200, true), Toggle(100, 6400, true))
            };
        }

        private static ScheduledToggle Toggle(int nodeId, int tick, bool enabled) =>
            new ScheduledToggle(nodeId, tick, enabled);

        private readonly struct RunResult
        {
            public RunResult(string finalCanonicalState, string traceHash)
            {
                FinalCanonicalState = finalCanonicalState;
                TraceHash = traceHash;
            }

            public string FinalCanonicalState { get; }
            public string TraceHash { get; }
        }

        private readonly struct ScheduledToggle
        {
            public ScheduledToggle(int nodeId, int tick, bool enabled)
            {
                NodeId = nodeId;
                Tick = tick;
                Enabled = enabled;
            }

            public int NodeId { get; }
            public int Tick { get; }
            public bool Enabled { get; }
        }

        private sealed class ScenarioDefinition
        {
            public ScenarioDefinition(string name, params ScheduledToggle[] toggles)
            {
                Name = name;
                Toggles = toggles ?? Array.Empty<ScheduledToggle>();
                Array.Sort(Toggles, (left, right) =>
                {
                    int comparison = left.Tick.CompareTo(right.Tick);
                    return comparison != 0 ? comparison : left.NodeId.CompareTo(right.NodeId);
                });
                Validate();
            }

            public string Name { get; }
            public ScheduledToggle[] Toggles { get; }

            private void Validate()
            {
                var touchedNodes = new HashSet<int>();
                var states = new Dictionary<int, bool>();
                foreach (ScheduledToggle toggle in Toggles)
                {
                    if (toggle.Tick <= 0 || toggle.Tick > TicksPerRun)
                        throw new ArgumentOutOfRangeException(nameof(Toggles),
                            $"Toggle tick {toggle.Tick} is outside the simulated range.");
                    bool previous = states.TryGetValue(toggle.NodeId, out bool enabled) ? enabled : true;
                    if (toggle.Enabled == previous)
                        throw new ArgumentException(
                            $"Node {toggle.NodeId} is redundantly set to {toggle.Enabled} at tick {toggle.Tick}.");
                    states[toggle.NodeId] = toggle.Enabled;
                    touchedNodes.Add(toggle.NodeId);
                }

                if (Toggles.Length == 0) return;
                if (touchedNodes.Count < 3 || touchedNodes.Count > 6)
                    throw new ArgumentException("Scheduled scenarios must toggle between three and six nodes.");
                foreach (KeyValuePair<int, bool> state in states)
                    if (!state.Value)
                        throw new ArgumentException($"Node {state.Key} is not re-enabled before the run ends.");
            }
        }
    }
}
