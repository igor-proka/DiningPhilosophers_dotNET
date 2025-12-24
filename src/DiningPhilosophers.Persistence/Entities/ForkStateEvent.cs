using System;

namespace DiningPhilosophers.Persistence.Entities
{
    public class ForkStateEvent
    {
        public long Id { get; set; }

        public Guid RunId { get; set; }
        public int ForkNumber { get; set; }

        // ("Available", "InUse")
        public string State { get; set; } = null!;
        public string? Owner { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public int? StepNumber { get; set; }
    }
}
