using System;
using System.Collections.Generic;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Strategies.Coordinators
{
    // Координатор, который специально создаёт дедлок.
    // Все философы одновременно берут левую вилку, вторая остаётся занята.
    public class StupidCoordinator : ICoordinator
    {
        public event Action<Philosopher, PhilosopherAction>? DecisionEvent;

        private readonly List<Philosopher> _philosophers;
        private readonly List<Fork> _forks;

        public StupidCoordinator(IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks)
        {
            _philosophers = new List<Philosopher>(philosophers);
            _forks = new List<Fork>(forks);
        }

        public void NotifyHungry(Philosopher philosopher)
        {
            // Даем всем философам действие "взять левую вилку"
            DecisionEvent?.Invoke(philosopher, PhilosopherAction.TakeLeftFork);
        }

        public void NotifyFinished(Philosopher philosopher)
        {
            // Для создания дедлока можно ничего не освобождать
        }
    }
}
