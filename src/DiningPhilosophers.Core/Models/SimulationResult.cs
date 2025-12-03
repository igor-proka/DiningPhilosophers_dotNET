using System.Collections.Generic;

namespace DiningPhilosophers.Core.Models
{
    public class ForkUtilizationInfo
    {
        public double FreePct { get; set; }
        public double BlockedPct { get; set; }
        public double InUsePct { get; set; }
    }

    public class SimulationResult
    {
        public int TotalSteps { get; set; }
        public long TotalMeals { get; set; }
        public double ThroughputPer1000 { get; set; }

        public Dictionary<string, long> WaitingTimes { get; } = new();
        public Dictionary<string, int> WaitingEpisodes { get; } = new();

        public Dictionary<int, ForkUtilizationInfo> ForkUtilizations { get; } = new();

        public string? DeadlockInfo { get; set; } = string.Empty;
    }
}