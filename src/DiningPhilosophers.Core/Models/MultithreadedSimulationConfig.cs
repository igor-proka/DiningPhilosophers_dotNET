namespace DiningPhilosophers.Core.Models
{
    public class MultithreadedSimulationConfig
    {
        public int DurationSeconds { get; init; } = 10;
        public int ThinkingTimeMin { get; init; } = 30;
        public int ThinkingTimeMax { get; init; } = 100;
        public int EatingTimeMin { get; init; } = 40;
        public int EatingTimeMax { get; init; } = 50;
        public int ForkAcquisitionTime { get; init; } = 20;
        public int DisplayInterval { get; init; } = 100;
    }
}