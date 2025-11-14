using System;

namespace DiningPhilosophers.Core.Models
{
    public class Philosopher
    {
        public string Name { get; }
        public PhilosopherState State { get; set; } = PhilosopherState.Thinking;

        // оставшиеся шаги в текущем состоянии (thinking/eating)
        public int StepsRemaining { get; set; } = 0;
        
        // Флаги владения вилками (модель знает только о своих вилках)
        public bool HasLeftFork { get; set; } = false;
        public bool HasRightFork { get; set; } = false;

        // Сейчас выбранное действие (устанавливается стратегией/координатором)
        public PhilosopherAction CurrentAction { get; set; } = PhilosopherAction.None;

        public Philosopher(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}