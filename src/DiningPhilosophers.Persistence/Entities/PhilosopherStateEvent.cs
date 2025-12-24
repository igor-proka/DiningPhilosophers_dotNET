using System;

namespace DiningPhilosophers.Persistence.Entities
{
    public class PhilosopherStateEvent
    {
        public long Id { get; set; }

        public Guid RunId { get; set; }
        public string PhilosopherName { get; set; } = null!;

        // ("Thinking", "Hungry", "Eating")
        public string State { get; set; } = null!;

        public int? StepsRemaining { get; set; }
        public bool? HasLeftFork { get; set; }
        public bool? HasRightFork { get; set; }
        public string? CurrentAction { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public int? StepNumber { get; set; } // for step-by-step sim, nullable
    }
}
