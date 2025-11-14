using System.Collections.Generic;

namespace DiningPhilosophers.Core.Contracts.Configuration
{
    public interface IPhilosopherNamesProvider
    {
        IEnumerable<string> GetNames();
    }
}
