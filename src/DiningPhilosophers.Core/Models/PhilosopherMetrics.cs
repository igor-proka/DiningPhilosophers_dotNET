namespace DiningPhilosophers.Core.Models
{
    public class PhilosopherMetrics
    {
        // Сколько раз этот философ съел (суммарно)
        public long MealsEaten { get; set; } = 0;

        // Сколько шагов он был в состоянии Hungry
        public long WaitingSteps { get; set; } = 0;

        public void Reset()
        {
            MealsEaten = 0;
            WaitingSteps = 0;
        }
    }
}
