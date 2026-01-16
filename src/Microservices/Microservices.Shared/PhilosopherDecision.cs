using DiningPhilosophers.Core.Models;

namespace Microservices.Shared
{
    public class PhilosopherDecision
    {
        public string PhilosopherName { get; set; } = string.Empty;
        public PhilosopherAction Action { get; set; }
    }
}