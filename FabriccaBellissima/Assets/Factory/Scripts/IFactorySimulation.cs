namespace FabriccaBellissima.Factory
{
    public interface IFactorySimulation
    {
        long Tick { get; }
        float TickDurationSeconds { get; }
        FactorySnapshot CurrentSnapshot { get; }
        void StepOneTick();
        void StepTicks(int count);
        void ResetSimulation();
        string ExportCanonicalSnapshot(bool pretty = false);
    }
}
