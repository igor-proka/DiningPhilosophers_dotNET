namespace DiningPhilosophers.Core.Models
{
    public class SimulationConfig
    {
        public int TotalSteps { get; init; } = 1_000_000;
        public int DisplayInterval { get; init; } = 1000;
        public int ThinkingTimeMin { get; init; } = 3;
        public int ThinkingTimeMax { get; init; } = 10;
        public int EatingTimeMin { get; init; } = 4;
        public int EatingTimeMax { get; init; } = 5;
        public int ForkAcquisitionTime { get; init; } = 2;

        public bool UseCoordinator { get; init; } = false;
        public CoordinatorType CoordinatorType { get; init; } = CoordinatorType.Semaphore;
    }
}