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
            scenario.nodes.Add(new FactoryNodeConfig
            {
                nodeId = 1,
                displayName = "Extension",
                kind = TestNodeFactory.TestKind,
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
    }
}
