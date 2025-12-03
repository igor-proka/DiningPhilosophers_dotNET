using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation;

namespace DiningPhilosophers.Services.Simulation.Multithreaded
{
    public class ThreadSafeForkAcquisitionManager
    {
        private readonly Dictionary<Philosopher, (int leftProgress, int rightProgress)> _progress 
            = new Dictionary<Philosopher, (int, int)>();
        private readonly int _acquisitionTime;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ThreadSafeForkAcquisitionManager(int acquisitionTime)
        {
            _acquisitionTime = acquisitionTime;
        }

        public void InitializePhilosopher(Philosopher philosopher)
        {
            _progress[philosopher] = (0, 0);
        }

        public async Task<bool> TryAcquireLeftForkAsync(Philosopher philosopher, ThreadSafeFork leftFork, CancellationToken ct)
        {
            // Синхронно пытаемся захватить вилку
            bool acquired = leftFork.TryAcquire(philosopher.Name);
            
            if (acquired)
            {
                // Симулируем задержку взятия вилки
                await Task.Delay(_acquisitionTime, ct);
                philosopher.HasLeftFork = true;
                return true;
            }
            return false;
        }

        public async Task<bool> TryAcquireRightForkAsync(Philosopher philosopher, ThreadSafeFork rightFork, CancellationToken ct)
        {
            bool acquired = rightFork.TryAcquire(philosopher.Name);
            
            if (acquired)
            {
                await Task.Delay(_acquisitionTime, ct);
                philosopher.HasRightFork = true;
                return true;
            }
            return false;
        }

        public void ResetProgress(Philosopher philosopher)
        {
            _semaphore.Wait();
            try
            {
                if (_progress.ContainsKey(philosopher))
                {
                    _progress[philosopher] = (0, 0);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}