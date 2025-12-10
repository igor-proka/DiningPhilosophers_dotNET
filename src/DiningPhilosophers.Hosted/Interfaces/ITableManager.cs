using System.Collections.Generic;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;

namespace DiningPhilosophers.Hosted.Interfaces
{
    public interface ITableManager
    {
        IList<Philosopher> GetPhilosophers();
        IList<ThreadSafeFork> GetForks();
        Philosopher GetPhilosopher(string name);
        ThreadSafeFork GetLeftFork(string name);
        ThreadSafeFork GetRightFork(string name);
        string[] GetPhilosopherNames();
    }
}