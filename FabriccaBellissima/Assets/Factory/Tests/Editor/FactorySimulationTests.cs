using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FabriccaBellissima.Factory.Tests
{
    public sealed class FactorySimulationTests
    {
        [Test]
        public void EndToEndProduction_ConservesResourcesAndConsumesPaintedBars()
        {
            FactorySimulation simulation = new FactorySimulation(BuildCompleteScenario());
            simulation.StepTicks(80);
            FactorySnapshot snapshot = simulation.CurrentSnapshot;

            Assert.That(snapshot.counters.sinkConsumed, Is.GreaterThan(0));
            Assert.That(snapshot.counters.ironBarsMade, Is.GreaterThanOrEqualTo(snapshot.counters.paintedBarsMade));
            Assert.That(snapshot.counters.paintedBarsMade, Is.GreaterThanOrEqualTo(snapshot.counters.sinkConsumed));
            Assert.That(snapshot.counters.chargesApplied, Is.EqualTo(snapshot.counters.paintedBarsMade * 2));
            Assert.That(snapshot.counters.coalConsumed, Is.LessThanOrEqualTo(snapshot.counters.coalGenerated));
            AssertUniqueOwnedItems(snapshot);
        }

        [Test]
        public void FullBelt_BlocksEntranceAndRetainsExactOutputIdentityUntilRecovery()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Belt(10, 2, 2, 1));
            scenario.nodes.Add(Belt(20, 4, 2, 2));
            scenario.nodes.Add(new FactoryNodeConfig
            {
                nodeId = 30, displayName = "Sink", kind = FactoryNodeKind.Sink, enabled = true, colorId = "Blue"
            });
            scenario.connections.Add(Connection(10, FactoryPorts.Output, 20, FactoryPorts.Input));
            scenario.connections.Add(Connection(20, FactoryPorts.Output, 30, FactoryPorts.Input));
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 10,
                beltItems = new List<FactoryBeltItem> { PaintedBeltItem(100, 2) }
            });
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 20,
                beltItems = new List<FactoryBeltItem>
                {
                    PaintedBeltItem(90, 4),
                    PaintedBeltItem(91, 2)
                }
            });
            scenario.nextItemId = 101;
            var simulation = new FactorySimulation(scenario);

            simulation.StepOneTick();
            // Source 10 resolves before source 20, so it is rejected while 20 is still full.
            Assert.That(Node(simulation, 10).beltItems.Single().item.id, Is.EqualTo(100));
            Assert.That(Node(simulation, 20).beltItems, Has.Count.EqualTo(1));
            Assert.That(Node(simulation, 30).consumedItemIds, Is.EqualTo(new[] { 90L }));

            simulation.SetNodeEnabled(20, false);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).beltItems.Single().item.id, Is.EqualTo(100));
            simulation.SetNodeEnabled(20, true);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).beltItems, Is.Empty);
            Assert.That(Node(simulation, 20).beltItems, Has.Count.EqualTo(1));
            Assert.That(Node(simulation, 20).beltItems.Last().item.id, Is.EqualTo(100));
            Assert.That(Node(simulation, 30).consumedItemIds, Is.EqualTo(new[] { 90L, 91L }));
            AssertUniqueOwnedItems(simulation.CurrentSnapshot);
        }

        [Test]
        public void Furnace_UsesLastFuelTickAndIgnitesQueuedCoalWithoutGap()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Furnace(10, burn: 2, smelt: 2));
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 10,
                workItem = Item(1, FactoryResourceType.IronOre),
                waitingCoal = Item(2, FactoryResourceType.Coal),
                fuelRemaining = 1
            });
            scenario.nextItemId = 3;
            var simulation = new FactorySimulation(scenario);

            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).workProgress, Is.EqualTo(1));
            Assert.That(Node(simulation, 10).fuelRemaining, Is.Zero);
            simulation.StepOneTick();
            FactoryNodeSnapshot furnace = Node(simulation, 10);
            Assert.That(furnace.workItem.resource, Is.EqualTo(FactoryResourceType.IronBar));
            Assert.That(furnace.workItem.id, Is.EqualTo(1));
            Assert.That(furnace.fuelRemaining, Is.EqualTo(1));
            Assert.That(furnace.waitingCoal, Is.Null);
            Assert.That(simulation.CurrentSnapshot.counters.coalConsumed, Is.EqualTo(1));
        }

        [Test]
        public void Furnace_StarvationPreservesProgressAndFuelBurnsWhileIdleOrBlocked()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Furnace(10, burn: 3, smelt: 3));
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 10, workItem = Item(1, FactoryResourceType.IronOre), fuelRemaining = 1
            });
            scenario.nextItemId = 2;
            var simulation = new FactorySimulation(scenario);
            simulation.StepTicks(4);
            Assert.That(Node(simulation, 10).workProgress, Is.EqualTo(1));

            FactoryScenario idle = EmptyScenario();
            idle.nodes.Add(Furnace(10, burn: 3, smelt: 1));
            idle.initialStates.Add(new FactoryNodeInitialState { nodeId = 10, fuelRemaining = 3 });
            var idleSimulation = new FactorySimulation(idle);
            idleSimulation.StepOneTick();
            Assert.That(Node(idleSimulation, 10).fuelRemaining, Is.EqualTo(2));
            idleSimulation.SetNodeEnabled(10, false);
            idleSimulation.StepTicks(3);
            Assert.That(Node(idleSimulation, 10).fuelRemaining, Is.EqualTo(2));
        }

        [Test]
        public void TwoCansPaintExactlyThreeBars_AndMissingSecondCanWaitsHalfway()
        {
            FactorySimulation complete = new FactorySimulation(PaintScenario(includeSecondCan: true));
            complete.StepTicks(30);
            FactorySnapshot completeSnapshot = complete.CurrentSnapshot;
            Assert.That(completeSnapshot.counters.sinkConsumed, Is.EqualTo(3));
            Assert.That(completeSnapshot.counters.cansOpened, Is.EqualTo(2));
            Assert.That(completeSnapshot.counters.chargesApplied, Is.EqualTo(6));
            Assert.That(Node(complete, 30).reservoirCharges, Is.Zero);

            FactorySimulation missing = new FactorySimulation(PaintScenario(includeSecondCan: false));
            missing.StepTicks(20);
            FactoryNodeSnapshot station = Node(missing, 30);
            Assert.That(missing.CurrentSnapshot.counters.sinkConsumed, Is.EqualTo(1));
            Assert.That(station.workItem.resource, Is.EqualTo(FactoryResourceType.IronBar));
            Assert.That(station.appliedCharges, Is.EqualTo(1));
            Assert.That(station.reservoirCharges, Is.Zero);
            Assert.That(station.status, Is.EqualTo("Waiting for paint"));
        }

        [Test]
        public void CanSlotAndReservoirCoexist_NoEarlyOpenAndExactSprayDuration()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Coloring(10, spray: 2, chargesPerBar: 3));
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 10,
                workItem = Item(1, FactoryResourceType.IronBar),
                waitingCan = Can(2, 3),
                reservoirCharges = 1,
                reservoirColorId = "Blue"
            });
            scenario.nextItemId = 3;
            var simulation = new FactorySimulation(scenario);

            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).sprayProgress, Is.EqualTo(1));
            Assert.That(Node(simulation, 10).waitingCan, Is.Not.Null);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).appliedCharges, Is.EqualTo(1));
            Assert.That(Node(simulation, 10).reservoirCharges, Is.Zero);
            Assert.That(Node(simulation, 10).waitingCan, Is.Not.Null);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).waitingCan, Is.Null);
            Assert.That(Node(simulation, 10).reservoirCharges, Is.EqualTo(3));
            Assert.That(Node(simulation, 10).sprayProgress, Is.EqualTo(1));
        }

        [Test]
        public void FinishedBlockedPaintedBarConsumesNoAdditionalPaint()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Coloring(10, spray: 1, chargesPerBar: 1));
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 10, workItem = Item(1, FactoryResourceType.IronBar),
                reservoirCharges = 3, reservoirColorId = "Blue"
            });
            scenario.nextItemId = 2;
            var simulation = new FactorySimulation(scenario);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).reservoirCharges, Is.EqualTo(2));
            Assert.That(Node(simulation, 10).workItem.resource, Is.EqualTo(FactoryResourceType.PaintedIronBar));
            simulation.StepTicks(10);
            Assert.That(Node(simulation, 10).reservoirCharges, Is.EqualTo(2));
            Assert.That(simulation.CurrentSnapshot.counters.chargesApplied, Is.EqualTo(1));
        }

        [Test]
        public void WrongResourceIsRejected_AndSinkConsumesValidBarImmediately()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Belt(10, 2, 2, 2));
            scenario.nodes.Add(new FactoryNodeConfig { nodeId = 20, displayName = "Sink", kind = FactoryNodeKind.Sink, enabled = true });
            scenario.connections.Add(Connection(10, FactoryPorts.Output, 20, FactoryPorts.Input));
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 10, beltItems = new List<FactoryBeltItem> { BeltItem(1, FactoryResourceType.IronBar, 2) }
            });
            scenario.nextItemId = 2;
            var simulation = new FactorySimulation(scenario);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).beltItems, Has.Count.EqualTo(1));
            Assert.That(Node(simulation, 20).sinkTotal, Is.Zero);

            scenario.initialStates[0].beltItems[0].item.resource = FactoryResourceType.PaintedIronBar;
            scenario.initialStates[0].beltItems[0].item.colorId = "Blue";
            simulation = new FactorySimulation(scenario);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).beltItems, Is.Empty);
            Assert.That(Node(simulation, 20).sinkTotal, Is.EqualTo(1));
            Assert.That(simulation.LastEvents.Last().kind, Is.EqualTo(FactoryEventKind.SinkConsumed));

            scenario.initialStates[0].beltItems[0].item.colorId = "Red";
            simulation = new FactorySimulation(scenario);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).beltItems, Has.Count.EqualTo(1));
            Assert.That(Node(simulation, 20).sinkTotal, Is.Zero);
        }

        [Test]
        public void DisablePauseAndResetPreserveProgressAndAllocator()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Extractor(10, FactoryResourceType.IronOre, 3));
            var simulation = new FactorySimulation(scenario);
            simulation.StepOneTick();
            simulation.SetNodeEnabled(10, false);
            simulation.StepTicks(4);
            Assert.That(Node(simulation, 10).workProgress, Is.EqualTo(1));
            simulation.SetNodeEnabled(10, true);
            simulation.StepTicks(2);
            Assert.That(Node(simulation, 10).outputItem.id, Is.EqualTo(1));
            simulation.ResetSimulation();
            Assert.That(simulation.Tick, Is.Zero);
            Assert.That(Node(simulation, 10).workProgress, Is.Zero);
            simulation.StepTicks(3);
            Assert.That(Node(simulation, 10).outputItem.id, Is.EqualTo(1));
        }

        [Test]
        public void CoordinatorPlayPauseSingleStepAndResetUseTheAuthoritativePath()
        {
            var gameObject = new UnityEngine.GameObject("Coordinator test");
            try
            {
                FactorySimulationCoordinator coordinator = gameObject.AddComponent<FactorySimulationCoordinator>();
                FactoryScenario scenario = EmptyScenario();
                scenario.nodes.Add(Extractor(10, FactoryResourceType.IronOre, 2));
                coordinator.Initialize(scenario);

                coordinator.Pause();
                Assert.That(coordinator.IsPlaying, Is.False);
                coordinator.StepOneTick();
                Assert.That(coordinator.CurrentSnapshot.tick, Is.EqualTo(1));
                coordinator.Play();
                Assert.That(coordinator.IsPlaying, Is.True);
                coordinator.Pause();
                coordinator.ResetSimulation();
                Assert.That(coordinator.IsPlaying, Is.False);
                Assert.That(coordinator.CurrentSnapshot.tick, Is.Zero);
                Assert.That(coordinator.CurrentSnapshot.nextItemId, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BatchesAndResetProduceByteEquivalentCanonicalState()
        {
            FactoryScenario scenario = BuildCompleteScenario();
            var individual = new FactorySimulation(scenario);
            for (int index = 0; index < 50; index++) individual.StepOneTick();
            string expected = individual.ExportCanonicalSnapshot();

            var batched = new FactorySimulation(scenario);
            batched.StepTicks(17);
            batched.StepTicks(3);
            batched.StepTicks(30);
            Assert.That(batched.ExportCanonicalSnapshot(), Is.EqualTo(expected));
            batched.ResetSimulation();
            batched.StepTicks(50);
            Assert.That(batched.ExportCanonicalSnapshot(), Is.EqualTo(expected));
        }

        [Test]
        public void NewlyTransferredItemCannotMoveOrTransferAgainUntilNextTick()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Extractor(10, FactoryResourceType.IronOre, 1));
            scenario.nodes.Add(Belt(20, 1, 1, 1));
            scenario.nodes.Add(Belt(30, 1, 1, 1));
            scenario.connections.Add(Connection(10, FactoryPorts.Output, 20, FactoryPorts.Input));
            scenario.connections.Add(Connection(20, FactoryPorts.Output, 30, FactoryPorts.Input));
            var simulation = new FactorySimulation(scenario);

            simulation.StepOneTick();
            Assert.That(Node(simulation, 20).beltItems.Single().progressUnits, Is.Zero);
            Assert.That(Node(simulation, 30).beltItems, Is.Empty);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 20).beltItems, Is.Empty);
            Assert.That(Node(simulation, 30).beltItems.Single().progressUnits, Is.Zero);
        }

        [Test]
        public void PositiveDurationCompletesInExactlyNEligibleIntervals()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Extractor(10, FactoryResourceType.IronOre, 3));
            var simulation = new FactorySimulation(scenario);
            simulation.StepTicks(2);
            Assert.That(Node(simulation, 10).outputItem, Is.Null);
            simulation.StepOneTick();
            Assert.That(Node(simulation, 10).outputItem, Is.Not.Null);
        }

        [Test]
        public void StableNodeOrderControlsSimultaneousIdsAndCanonicalScenarioOrdering()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Extractor(20, FactoryResourceType.Coal, 1));
            scenario.nodes.Add(Extractor(10, FactoryResourceType.IronOre, 1));
            var simulation = new FactorySimulation(scenario);

            simulation.StepOneTick();

            Assert.That(Node(simulation, 10).outputItem.id, Is.EqualTo(1));
            Assert.That(Node(simulation, 20).outputItem.id, Is.EqualTo(2));
            Assert.That(simulation.LastEvents.Select(value => value.nodeId), Is.EqualTo(new[] { 10, 20 }));
            string json = scenario.ToJson(false);
            Assert.That(json.IndexOf("\"nodeId\":10", StringComparison.Ordinal),
                Is.LessThan(json.IndexOf("\"nodeId\":20", StringComparison.Ordinal)));
        }

        [Test]
        public void CanonicalSnapshotContainsConfigurationConnectionsAndRejectsInvalidPorts()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Extractor(10, FactoryResourceType.IronOre, 2));
            scenario.nodes.Add(Belt(20, 4, 1, 2));
            scenario.connections.Add(Connection(10, FactoryPorts.Output, 20, FactoryPorts.Input));
            var simulation = new FactorySimulation(scenario);
            FactorySnapshot snapshot = simulation.CurrentSnapshot;
            Assert.That(snapshot.tickDurationMicroseconds, Is.EqualTo(50000));
            Assert.That(snapshot.connections, Has.Count.EqualTo(1));
            Assert.That(snapshot.nodes.Single(node => node.nodeId == 10).configuredResource,
                Is.EqualTo(FactoryResourceType.IronOre));
            Assert.That(snapshot.nodes.Single(node => node.nodeId == 10).durationTicks, Is.EqualTo(2));

            scenario.connections[0].sourcePortId = 999;
            Assert.Throws<ArgumentException>(() => new FactorySimulation(scenario));
        }

        [Test]
        public void DefaultAuthoringConversionsMatchDocumentedValues()
        {
            var gameObject = new UnityEngine.GameObject("Authoring test");
            try
            {
                FactoryDemoAuthoring authoring = gameObject.AddComponent<FactoryDemoAuthoring>();
                authoring.ResetToDefaults();
                FactoryScenario scenario = authoring.CompileScenario();
                Assert.That(scenario.nodes.Single(node => node.nodeId == 10).durationTicks, Is.EqualTo(40));
                Assert.That(scenario.nodes.Single(node => node.nodeId == 30).durationTicks, Is.EqualTo(120));
                FactoryNodeConfig belt = scenario.nodes.Single(node => node.nodeId == 20);
                Assert.That(belt.lengthUnits, Is.EqualTo(4000));
                Assert.That(belt.movementPerTick, Is.EqualTo(50));
                Assert.That(belt.spacingUnits, Is.EqualTo(1000));

                var simulation = new FactorySimulation(scenario);
                simulation.StepTicks(600);
                Assert.That(simulation.CurrentSnapshot.counters.sinkConsumed, Is.GreaterThan(0),
                    "The documented default scenario must visibly complete its production chain.");
                AssertUniqueOwnedItems(simulation.CurrentSnapshot);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static FactoryScenario BuildCompleteScenario()
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Extractor(10, FactoryResourceType.IronOre, 1));
            scenario.nodes.Add(Belt(20, 4, 4, 4));
            scenario.nodes.Add(Extractor(30, FactoryResourceType.Coal, 2));
            scenario.nodes.Add(Belt(40, 4, 4, 4));
            scenario.nodes.Add(Furnace(50, 6, 2));
            scenario.nodes.Add(Belt(60, 4, 4, 4));
            FactoryNodeConfig paint = Extractor(70, FactoryResourceType.PaintCan, 2);
            paint.paintCanCharges = 3;
            paint.colorId = "Blue";
            scenario.nodes.Add(paint);
            scenario.nodes.Add(Belt(80, 4, 4, 4));
            scenario.nodes.Add(Coloring(90, 1, 2));
            scenario.nodes.Add(Belt(100, 4, 4, 4));
            scenario.nodes.Add(new FactoryNodeConfig { nodeId = 110, displayName = "Sink", kind = FactoryNodeKind.Sink, enabled = true });
            scenario.connections.AddRange(new[]
            {
                Connection(10, 20, 20, 10), Connection(20, 20, 50, 10),
                Connection(30, 20, 40, 10), Connection(40, 20, 50, 20),
                Connection(50, 30, 60, 10), Connection(60, 20, 90, 10),
                Connection(70, 20, 80, 10), Connection(80, 20, 90, 20),
                Connection(90, 30, 100, 10), Connection(100, 20, 110, 10)
            });
            return scenario;
        }

        private static FactoryScenario PaintScenario(bool includeSecondCan)
        {
            FactoryScenario scenario = EmptyScenario();
            scenario.nodes.Add(Belt(10, 3, 3, 3));
            scenario.nodes.Add(Belt(20, 2, 2, 2));
            scenario.nodes.Add(Coloring(30, 1, 2));
            scenario.nodes.Add(new FactoryNodeConfig { nodeId = 40, displayName = "Sink", kind = FactoryNodeKind.Sink, enabled = true });
            scenario.connections.Add(Connection(10, FactoryPorts.Output, 30, FactoryPorts.ColoringBar));
            scenario.connections.Add(Connection(20, FactoryPorts.Output, 30, FactoryPorts.ColoringCan));
            scenario.connections.Add(Connection(30, FactoryPorts.ColoringOutput, 40, FactoryPorts.Input));
            scenario.initialStates.Add(new FactoryNodeInitialState
            {
                nodeId = 10,
                beltItems = new List<FactoryBeltItem>
                {
                    BeltItem(1, FactoryResourceType.IronBar, 3),
                    BeltItem(2, FactoryResourceType.IronBar, 2),
                    BeltItem(3, FactoryResourceType.IronBar, 1)
                }
            });
            var cans = new List<FactoryBeltItem> { new FactoryBeltItem { item = Can(4, 3), progressUnits = 2 } };
            if (includeSecondCan) cans.Add(new FactoryBeltItem { item = Can(5, 3), progressUnits = 1 });
            scenario.initialStates.Add(new FactoryNodeInitialState { nodeId = 20, beltItems = cans });
            scenario.nextItemId = includeSecondCan ? 6 : 5;
            return scenario;
        }

        private static FactoryScenario EmptyScenario() => new FactoryScenario
        {
            tickDurationMicroseconds = 50000, nextItemId = 1
        };
        private static FactoryNodeConfig Extractor(int id, FactoryResourceType resource, int duration) => new FactoryNodeConfig
        {
            nodeId = id, displayName = "Extractor", kind = FactoryNodeKind.Extractor, enabled = true,
            resource = resource, durationTicks = duration, colorId = "Blue", paintCanCharges = 3
        };
        private static FactoryNodeConfig Belt(int id, int length, int movement, int capacity) => new FactoryNodeConfig
        {
            nodeId = id, displayName = "Belt", kind = FactoryNodeKind.Conveyor, enabled = true,
            lengthUnits = length, movementPerTick = movement, capacity = capacity,
            spacingUnits = Math.Max(1, length / capacity)
        };
        private static FactoryNodeConfig Furnace(int id, int burn, int smelt) => new FactoryNodeConfig
        {
            nodeId = id, displayName = "Furnace", kind = FactoryNodeKind.Furnace, enabled = true,
            burnTicks = burn, smeltTicks = smelt
        };
        private static FactoryNodeConfig Coloring(int id, int spray, int chargesPerBar) => new FactoryNodeConfig
        {
            nodeId = id, displayName = "Coloring", kind = FactoryNodeKind.ColoringStation, enabled = true,
            colorId = "Blue", sprayTicks = spray, chargesPerBar = chargesPerBar
        };
        private static FactoryConnection Connection(int source, int sourcePort, int destination, int destinationPort) =>
            new FactoryConnection { sourceNodeId = source, sourcePortId = sourcePort,
                destinationNodeId = destination, destinationPortId = destinationPort };
        private static FactoryItem Item(long id, FactoryResourceType resource) =>
            new FactoryItem { id = id, resource = resource };
        private static FactoryItem Can(long id, int charges) =>
            new FactoryItem { id = id, resource = FactoryResourceType.PaintCan, colorId = "Blue", charges = charges };
        private static FactoryBeltItem BeltItem(long id, FactoryResourceType resource, int progress) =>
            new FactoryBeltItem { item = Item(id, resource), progressUnits = progress };
        private static FactoryBeltItem PaintedBeltItem(long id, int progress) => new FactoryBeltItem
        {
            item = new FactoryItem
            {
                id = id, resource = FactoryResourceType.PaintedIronBar, colorId = "Blue"
            },
            progressUnits = progress
        };
        private static FactoryNodeSnapshot Node(FactorySimulation simulation, int nodeId) =>
            simulation.CurrentSnapshot.nodes.Single(node => node.nodeId == nodeId);

        private static void AssertUniqueOwnedItems(FactorySnapshot snapshot)
        {
            var ids = new List<long>();
            foreach (FactoryNodeSnapshot node in snapshot.nodes)
            {
                if (node.outputItem != null) ids.Add(node.outputItem.id);
                if (node.workItem != null) ids.Add(node.workItem.id);
                if (node.waitingCoal != null) ids.Add(node.waitingCoal.id);
                if (node.waitingCan != null) ids.Add(node.waitingCan.id);
                ids.AddRange(node.beltItems.Select(item => item.item.id));
            }
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }
    }
}
