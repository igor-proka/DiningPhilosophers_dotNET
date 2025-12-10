using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Simulation
{
    public class DeadlockChecker
    {
        public bool CheckDeadlock(IList<Philosopher> philosophers)
        {
            if (philosophers.Count == 0) return false;
            
            return philosophers.All(p => p.State == PhilosopherState.Hungry) &&
                   philosophers.All(p => p.HasLeftFork ^ p.HasRightFork);
        }
    }
}