using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;

namespace DiningPhilosophers.Services.Metrics
{
    public class MultithreadedMetricsCollector : IMultithreadedMetricsCollector
    {
        private readonly Dictionary<string, MultithreadedPhilosopherMetrics> _philos = new Dictionary<string, MultithreadedPhilosopherMetrics>();
        private readonly Dictionary<int, ForkMetrics> _forks = new Dictionary<int, ForkMetrics>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly List<ThreadSafeFork> _threadSafeForks;
        private readonly List<Philosopher> _philosophers;
        private long _totalObservations = 0;
        private DateTime _simulationStartTime;

        public MultithreadedMetricsCollector(IEnumerable<Philosopher> philosophers, IEnumerable<ThreadSafeFork> forks)
        {
            _philosophers = philosophers.ToList();
            _threadSafeForks = forks.ToList();
            _simulationStartTime = DateTime.Now;

            foreach (var p in _philosophers)
                _philos[p.Name] = new MultithreadedPhilosopherMetrics();

            foreach (var f in _threadSafeForks)
                _forks[f.Id] = new ForkMetrics();
        }

        public MultithreadedPhilosopherMetrics GetPhilosopherMetrics(string name)
        {
            _lock.EnterReadLock();
            try { return _philos[name]; }
            finally { _lock.ExitReadLock(); }
        }

        public ForkMetrics GetForkMetrics(int forkId)
        {
            _lock.EnterReadLock();
            try { return _forks[forkId]; }
            finally { _lock.ExitReadLock(); }
        }

        public void StartWaiting(string name)
        {
            _lock.EnterWriteLock();
            try 
            { 
                var metrics = _philos[name];
                if (metrics.HungerStartTime == null)
                {
                    metrics.HungerStartTime = DateTime.Now;
                    metrics.HungerEpisodes++;
                }
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void StopWaiting(string name)
        {
            _lock.EnterWriteLock();
            try 
            { 
                var metrics = _philos[name];
                if (metrics.HungerStartTime.HasValue)
                {
                    var waitTime = DateTime.Now - metrics.HungerStartTime.Value;
                    metrics.TotalWaitingTimeMs += (long)waitTime.TotalMilliseconds;
                    metrics.HungerStartTime = null;
                }
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void IncrementMeal(string name)
        {
            _lock.EnterWriteLock();
            try { _philos[name].MealsEaten++; }
            finally { _lock.ExitWriteLock(); }
        }

        public void UpdateMetrics()
        {
            _lock.EnterWriteLock();
            try
            {
                _totalObservations++;

                foreach (var fork in _threadSafeForks)
                {
                    var fm = _forks[fork.Id];
                    
                    if (fork.State == ForkState.Available)
                    {
                        fm.StepsFree++;
                    }
                    else if (string.IsNullOrEmpty(fork.Owner))
                    {
                        fm.StepsBlocked++;
                    }
                    else
                    {
                        var owner = _philosophers.FirstOrDefault(p => p.Name == fork.Owner);
                        if (owner != null && owner.State == PhilosopherState.Eating)
                        {
                            fm.StepsInUse++;
                        }
                        else
                        {
                            fm.StepsBlocked++;
                        }
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Reset()
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var p in _philos.Values) p.Reset();
                foreach (var f in _forks.Values) f.Reset();
                _totalObservations = 0;
                _simulationStartTime = DateTime.Now;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public long GetTotalObservations() => _totalObservations;

        // Для обратной совместимости с IMetricsCollector
        public void IncrementWaiting(string name)
        {
            // Не используется в многопоточной версии
        }
    }
}