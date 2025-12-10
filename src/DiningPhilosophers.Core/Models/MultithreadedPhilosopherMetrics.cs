namespace DiningPhilosophers.Core.Models
{
    public class MultithreadedPhilosopherMetrics
    {
        // Сколько раз этот философ съел (суммарно)
        public long MealsEaten { get; set; } = 0;

        // СУММАРНОЕ время ожидания в миллисекундах
        public long TotalWaitingTimeMs { get; set; } = 0;

        // Время когда философ стал голодным (для расчета продолжительности ожидания)
        public DateTime? HungerStartTime { get; set; }

        // Количество эпизодов голода
        public int HungerEpisodes { get; set; } = 0;

        public void Reset()
        {
            MealsEaten = 0;
            TotalWaitingTimeMs = 0;
            HungerStartTime = null;
            HungerEpisodes = 0;
        }
    }
}