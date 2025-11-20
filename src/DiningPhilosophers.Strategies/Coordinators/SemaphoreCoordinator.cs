using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Strategies.Coordinators
{
    public class SemaphoreCoordinator : ICoordinator
    {
        private readonly Dictionary<Philosopher, (Fork Left, Fork Right)> _philosopherForks;
        private readonly Queue<Philosopher> _waitingQueue = new();
        private readonly HashSet<Philosopher> _eatingPhilosophers = new();
        private const int MaxEating = 4;

        public event Action<Philosopher, PhilosopherAction>? DecisionEvent;

        public SemaphoreCoordinator(IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks)
        {
            _philosopherForks = CreatePhilosopherForksMapping(philosophers, forks);
        }

        public void NotifyHungry(Philosopher philosopher)
        {
            // Если философ уже ест или ждет - игнорируем
            if (_eatingPhilosophers.Contains(philosopher) || _waitingQueue.Contains(philosopher))
                return;

            _waitingQueue.Enqueue(philosopher);
            TryGrantForks();
        }

        public void NotifyFinished(Philosopher philosopher)
        {
            _eatingPhilosophers.Remove(philosopher);
            // Когда философ закончил есть, пробуем раздать вилки другим
            TryGrantForks();
        }

        private void TryGrantForks()
        {
            // Не можем раздавать вилки если достигли максимума
            if (_eatingPhilosophers.Count >= MaxEating)
                return;

            // Проходим по очереди и пытаемся выдать вилки
            var tempQueue = new Queue<Philosopher>();
            
            while (_waitingQueue.Count > 0)
            {
                var philosopher = _waitingQueue.Dequeue();
                var (leftFork, rightFork) = _philosopherForks[philosopher];

                // Проверяем доступность ВСЕХ нужных вилок
                if (leftFork.State == ForkState.Available && rightFork.State == ForkState.Available)
                {
                    // Захватываем вилки
                    leftFork.State = ForkState.InUse;
                    leftFork.Owner = philosopher.Name;
                    rightFork.State = ForkState.InUse;
                    rightFork.Owner = philosopher.Name;

                    _eatingPhilosophers.Add(philosopher);
                    
                    // Немедленно сообщаем философу взять обе вилки
                    DecisionEvent?.Invoke(philosopher, PhilosopherAction.TakeLeftFork | PhilosopherAction.TakeRightFork);
                    
                    // Если достигли максимума едящих - выходим
                    if (_eatingPhilosophers.Count >= MaxEating)
                        break;
                }
                else
                {
                    // Вилки недоступны - оставляем в очереди
                    tempQueue.Enqueue(philosopher);
                }
            }

            // Возвращаем необработанных философов обратно в очередь
            while (tempQueue.Count > 0)
            {
                _waitingQueue.Enqueue(tempQueue.Dequeue());
            }
        }

        private Dictionary<Philosopher, (Fork Left, Fork Right)> CreatePhilosopherForksMapping(
            IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks)
        {
            var mapping = new Dictionary<Philosopher, (Fork, Fork)>();
            var philosophersList = philosophers.ToList();
            var forksList = forks.ToList();
            
            for (int i = 0; i < philosophersList.Count; i++)
            {
                var philosopher = philosophersList[i];
                var leftFork = forksList[(i + forksList.Count - 1) % forksList.Count];
                var rightFork = forksList[i % forksList.Count];
                
                mapping[philosopher] = (leftFork, rightFork);
            }
            
            return mapping;
        }
    }
}