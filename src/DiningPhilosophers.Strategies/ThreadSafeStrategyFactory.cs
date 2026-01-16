using System.Collections.Generic;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using DiningPhilosophers.Strategies.Coordinators;

namespace DiningPhilosophers.Strategies
{
    public class ThreadSafeStrategyFactory
    {
        public (IPhilosopherStrategy strategy, ICoordinator? coordinator) Create(
            bool useCoordinator, 
            CoordinatorType coordinatorType,
            IEnumerable<Philosopher> philosophers, 
            IEnumerable<ThreadSafeFork> threadSafeForks)
        {
            // Создаём snapshot Fork из ThreadSafeFork для координатора (чтобы избежать CA2021 и несоответствия типов)
            var forks = new List<Fork>();
            foreach (var tsf in threadSafeForks)
            {
                forks.Add(new Fork(tsf.Id)
                {
                    State = tsf.State,
                    Owner = tsf.Owner
                });
            }

            IPhilosopherStrategy strategy = new HierarchyStrategy();
            ICoordinator? coordinator = null;

            if (useCoordinator)
            {
                coordinator = coordinatorType switch
                {
                    CoordinatorType.Stupid => new StupidCoordinator(philosophers, forks),
                    CoordinatorType.Semaphore => new SemaphoreCoordinator(philosophers, forks),
                    _ => new SemaphoreCoordinator(philosophers, forks)
                };

                strategy = new CoordinatorStrategy();
            }

            if (coordinator == null && useCoordinator)
            {
                throw new InvalidOperationException("Coordinator was not created despite useCoordinator being true.");
            }

            return (strategy, coordinator);
        }
    }
}