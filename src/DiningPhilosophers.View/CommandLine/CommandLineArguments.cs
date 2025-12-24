namespace DiningPhilosophers.View.CommandLine
{
    public class CommandLineArguments
    {
        public Guid RunId { get; set; }
        public double DelaySeconds { get; set; }
        public DateTime StartedAtUtc { get; set; }
        
        public DateTime RequestedTimeUtc => StartedAtUtc.AddSeconds(DelaySeconds);
        
        public bool IsValid => RunId != Guid.Empty && DelaySeconds >= 0;
    }
}