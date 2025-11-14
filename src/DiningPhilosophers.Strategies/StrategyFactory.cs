using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Strategies;
using DiningPhilosophers.Strategies.Coordinators;

namespace DiningPhilosophers.Strategies
{
    public class StrategyFactory : IStrategyFactory
    {
        public (IPhilosopherStrategy strategy, ICoordinator? coordinator) Create(
            bool useCoordinator, 
            CoordinatorType coordinatorType,
            IEnumerable<Philosopher> philosophers, 
            IEnumerable<Fork> forks)
        {
            IPhilosopherStrategy strategy = new NaiveStrategy();
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

            return (strategy, coordinator);
        }
    }
}