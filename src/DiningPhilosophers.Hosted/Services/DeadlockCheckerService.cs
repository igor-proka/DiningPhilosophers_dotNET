using System;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Services.Simulation;
using Microsoft.Extensions.Hosting;

namespace DiningPhilosophers.Hosted.Services
{
    public class DeadlockCheckerService : BackgroundService
    {
        private readonly ITableManager _tableManager;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly DeadlockChecker _deadlockChecker;

        public DeadlockCheckerService(
            ITableManager tableManager,
            IHostApplicationLifetime lifetime)
        {
            _tableManager = tableManager;
            _lifetime = lifetime;
            _deadlockChecker = new DeadlockChecker();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_deadlockChecker.CheckDeadlock(_tableManager.GetPhilosophers()))
                {
                    Console.WriteLine("\nDEADLOCK detected: all philosophers hungry and each holds exactly one fork.");
                    _lifetime.StopApplication();
                    break;
                }
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}