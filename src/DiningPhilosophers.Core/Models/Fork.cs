namespace DiningPhilosophers.Core.Models
{
    public class Fork
    {
        public int Id { get; }
        public ForkState State { get; set; } = ForkState.Available;
        public string? Owner { get; set; }

        public Fork(int id) => Id = id;
    }
}