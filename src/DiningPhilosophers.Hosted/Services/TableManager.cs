using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Configuration;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Services.Simulation.Multithreaded;

namespace DiningPhilosophers.Hosted.Services
{
    public class TableManager : ITableManager
    {
        private readonly Dictionary<string, Philosopher> _philosophers = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ThreadSafeFork> _forks;
        private readonly Dictionary<string, (ThreadSafeFork Left, ThreadSafeFork Right)> _forkMapping = new(StringComparer.OrdinalIgnoreCase);
        private readonly string[] _names;

        public TableManager(IPhilosopherNamesProvider namesProvider)
        {
            _names = namesProvider.GetNames().ToArray();

            // Создаем философов
            var philosopherList = _names.Select(n => new Philosopher(n)).ToList();

            // Создаем вилки
            _forks = Enumerable.Range(1, _names.Length).Select(i => new ThreadSafeFork(i)).ToList();

            // Маппинг
            for (int i = 0; i < _names.Length; i++)
            {
                var name = _names[i];
                _philosophers[name] = philosopherList[i];

                var leftIndex = (i + _names.Length - 1) % _names.Length;
                var rightIndex = i % _names.Length;

                _forkMapping[name] = (_forks[leftIndex], _forks[rightIndex]);
            }
        }

        public IList<Philosopher> GetPhilosophers() => _philosophers.Values.ToList();

        public IList<ThreadSafeFork> GetForks() => _forks;

        public Philosopher GetPhilosopher(string name) => _philosophers[name];

        public ThreadSafeFork GetLeftFork(string name) => _forkMapping[name].Left;

        public ThreadSafeFork GetRightFork(string name) => _forkMapping[name].Right;

        public string[] GetPhilosopherNames() => _names;
    }
}