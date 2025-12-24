using System.Collections.Generic;
using DiningPhilosophers.Persistence.Entities;
using DiningPhilosophers.View.CommandLine;
using DiningPhilosophers.View.Models;

namespace DiningPhilosophers.View.Renderers
{
    public interface ISimulationStateRenderer
    {
        void Render(CommandLineArguments arguments, SimulationRun run, 
            IEnumerable<PhilosopherStateEvent> philosophers, IEnumerable<ForkStateEvent> forks,
            string? warningMessage = null);
    }
}