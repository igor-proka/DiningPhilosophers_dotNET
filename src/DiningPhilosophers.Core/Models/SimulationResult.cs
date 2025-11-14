using System.Collections.Generic;

namespace DiningPhilosophers.Core.Models
{
    public class SimulationResult
    {
        public long TotalMeals { get; set; }
        public double ThroughputPer1000 { get; set; }
        public Dictionary<string, long> WaitingTimes { get; set; } = new();
        public Dictionary<int, double> ForkUtilizations { get; set; } = new();
        public string? DeadlockInfo { get; set; }
        public int TotalSteps { get; set; }
    }
}