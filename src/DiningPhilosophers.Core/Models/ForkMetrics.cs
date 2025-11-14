namespace DiningPhilosophers.Core.Models
{
    public class ForkMetrics
    {
        // Сколько шагов в статусе Available
        public long StepsFree { get; set; } = 0;

        // Сколько шагов держится философом, но он НЕ ест (blocked)
        public long StepsBlocked { get; set; } = 0;

        // Сколько шагов используется для еды (owner is eating)
        public long StepsInUse { get; set; } = 0;

        public long TotalObservedSteps => StepsFree + StepsBlocked + StepsInUse;

        public void Reset()
        {
            StepsFree = 0;
            StepsBlocked = 0;
            StepsInUse = 0;
        }
    }
}
