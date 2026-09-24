using System;
using NUnit.Framework;

namespace FabriccaBellissima.Factory.Tests
{
    public sealed class FactoryArchitectureTests
    {
        [Test]
        public void RegisteredExtensionNodeRunsWithoutCentralSimulationChanges()
        {
            var registry = new FactoryNodeFactoryRegistry(new IFactoryNodeFactory[] { new TestNodeFactory() });
            var scenario = new FactoryScenario();
            scenario.nodes.Add(new TestNodeConfig
            {
                nodeId = 1,
                displayName = "Extension",
                enabled = true
            });

            var simulation = new FactorySimulation(scenario, registry,
                new FactoryTransferResolver(), new FactorySnapshotBuilder());
            simulation.StepOneTick();

            Assert.That(simulation.CurrentSnapshot.nodes[0].workProgress, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateNodeFactoryRegistrationFailsClearly()
        {
            Assert.Throws<ArgumentException>(() => new FactoryNodeFactoryRegistry(new IFactoryNodeFactory[]
            {
                new TestNodeFactory(),
                new TestNodeFactory()
            }));
        }

        [Test]
        public void FactoryRejectsWrongConfigurationTypeForDeclaredKind()
        {
            var config = new ExtractorNodeConfig
            {
                nodeId = 1,
                displayName = "Mismatched",
                durationTicks = 1,
                resource = FactoryResourceType.IronOre,
                kind = FactoryNodeKind.Furnace
            };

            var registry = FactoryNodeFactoryRegistry.CreateDefault();
            ArgumentException exception = Assert.Throws<ArgumentException>(() => registry.Validate(config));
            Assert.That(exception.Message, Does.Contain(nameof(FurnaceNodeConfig)));
        }

        [Test]
        public void ScenarioSerializationPreservesConcreteNodeConfiguration()
        {
            var scenario = new FactoryScenario();
            scenario.nodes.Add(new FurnaceNodeConfig
            {
                nodeId = 10,
                displayName = "Furnace",
                burnTicks = 8,
                smeltTicks = 3
            });

            string json = scenario.ToJson(false);
            FactoryScenario restored = FactoryScenario.FromJson(json);

            Assert.That(restored.nodes, Has.Count.EqualTo(1));
            Assert.That(restored.nodes[0], Is.TypeOf<FurnaceNodeConfig>());
            var config = (FurnaceNodeConfig)restored.nodes[0];
            Assert.That(config.burnTicks, Is.EqualTo(8));
            Assert.That(config.smeltTicks, Is.EqualTo(3));
            Assert.That(json, Does.Not.Contain("sprayTicks"));
            Assert.That(json, Does.Not.Contain("chargesPerBar"));
        }
    }
}
