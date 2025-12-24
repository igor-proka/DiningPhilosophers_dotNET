using System;
using System.Collections.Generic;

namespace DiningPhilosophers.Persistence.Entities
{
    public class SimulationRun
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAtUtc { get; set; }
        public string? OptionsJson { get; set; }

        public List<PhilosopherStateEvent> PhilosopherStateEvents { get; set; } = new();
        public List<ForkStateEvent> ForkStateEvents { get; set; } = new();
    }
}
