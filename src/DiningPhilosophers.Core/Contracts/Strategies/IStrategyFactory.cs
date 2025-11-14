using System.Collections.Generic;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Strategies
{
    public interface IStrategyFactory
    {
        (IPhilosopherStrategy strategy, ICoordinator? coordinator) Create(
            bool useCoordinator, 
            CoordinatorType coordinatorType,
            IEnumerable<Philosopher> philosophers, 
            IEnumerable<Fork> forks);
    }
}