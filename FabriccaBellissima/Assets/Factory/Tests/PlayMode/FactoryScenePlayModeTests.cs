using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FabriccaBellissima.Factory.Tests
{
    public sealed class FactoryScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator GeneratedMainSceneBootsAdvancesAndCreatesPresentation()
        {
            SceneManager.LoadScene("MainGameScene", LoadSceneMode.Single);
            yield return null;

            FactorySimulationCoordinator coordinator = Object.FindFirstObjectByType<FactorySimulationCoordinator>();
            Assert.That(coordinator, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FactoryDemoAuthoring>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FactoryPresentation>(), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(coordinator.Simulation, Is.Not.Null);
            Assert.That(coordinator.Simulation.NodeConfigs, Has.Count.EqualTo(11));
            Assert.That(coordinator.Simulation.Connections, Has.Count.EqualTo(10));

            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(coordinator.CurrentSnapshot.tick, Is.GreaterThan(0));
            Transform presentation = coordinator.transform.Find("Runtime Factory Presentation (non-authoritative)");
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.childCount, Is.GreaterThan(20));
        }
    }
}
