namespace Microservices.Shared
{
    public class HungryEvent
    {
        public string PhilosopherName { get; set; } = string.Empty;
        public string LeftForkId { get; set; } = string.Empty;
        public string RightForkId { get; set; } = string.Empty;
    }
}