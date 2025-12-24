using System;
using System.Collections.Generic;
using DiningPhilosophers.Persistence.Entities;

namespace DiningPhilosophers.View.Models
{
    public class SimulationStateResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }
        
        public SimulationRun? Run { get; set; }
        public IReadOnlyList<PhilosopherStateEvent> Philosophers { get; set; } = new List<PhilosopherStateEvent>();
        public IReadOnlyList<ForkStateEvent> Forks { get; set; } = new List<ForkStateEvent>();
        
        public double RequestedDelay { get; set; }
        public double ActualDelay { get; set; }
    }
}