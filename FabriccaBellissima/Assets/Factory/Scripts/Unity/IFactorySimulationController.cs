using R3;
using System.Collections.Generic;

namespace FabriccaBellissima.Factory
{
    public interface IFactorySimulationController
    {
        bool IsPlaying { get; }
        float SimulationSpeed { get; }
        FactorySnapshot CurrentSnapshot { get; }
        IReadOnlyList<FactoryNodeConfig> NodeConfigs { get; }
        Observable<FactorySnapshot> Snapshots { get; }
        void StepOneTick();
        void StepTicks(int count);
        void Play();
        void Pause();
        void ResetSimulation();
        void SetSimulationSpeed(float speed);
        void SetNodeEnabled(int nodeId, bool enabled);
        string ExportCanonicalSnapshot(bool pretty = true);
    }
}
