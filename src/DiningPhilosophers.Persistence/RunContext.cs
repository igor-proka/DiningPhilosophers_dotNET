using System;

namespace DiningPhilosophers.Persistence
{
    public sealed class RunContext
    {
        public Guid RunId { get; set; } = Guid.Empty;
        public DateTime StartedAtUtc { get; set; } = DateTime.MinValue;
        public bool IsInitialized => RunId != Guid.Empty;
    }
}
