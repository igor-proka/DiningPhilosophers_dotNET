namespace Microservices.Shared
{
    public class ForkRequest
    {
        public string ForkId { get; set; } = string.Empty;
        public ForkAction Action { get; set; }
        public string PhilosopherName { get; set; } = string.Empty;
    }
}