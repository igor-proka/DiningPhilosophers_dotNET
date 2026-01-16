namespace Microservices.Shared
{
    public class MetricsRequest
    {
        public string PhilosopherName { get; set; } = string.Empty;
        public long MealsEaten { get; set; }
        public long TotalWaitingTimeMs { get; set; }

        public DateTime? HungerStartTime { get; set; }
        public int HungerEpisodes { get; set; } = 0;
    }
}