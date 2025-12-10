using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Simulation;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Metrics;

namespace DiningPhilosophers.Services.Simulation.Multithreaded
{
    public class MultithreadedSimulationEngine : ISimulation
    {
        private readonly MultithreadedSimulationConfig _config;
        private readonly IMonitor _monitor;
        private readonly IPhilosopherStrategy _strategy;
        private readonly List<ThreadSafeFork> _forks;
        private readonly List<Philosopher> _philosophers;
        private readonly MultithreadedMetricsCollector _metrics;
        private readonly MultithreadedToMetricsAdapter _metricsAdapter;
        private SimulationResult _result = new SimulationResult();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly DateTime _startTime;
        private readonly DeadlockChecker _deadlockChecker;

        public MultithreadedSimulationEngine(
            MultithreadedSimulationConfig config,
            IMonitor monitor,
            IPhilosopherStrategy strategy,
            IEnumerable<Philosopher> philosophers,
            IEnumerable<ThreadSafeFork> forks)
        {
            _config = config;
            _monitor = monitor;
            _strategy = strategy;
            _philosophers = philosophers.ToList();
            _forks = forks.ToList();
            _metrics = new MultithreadedMetricsCollector(_philosophers, _forks);
            _metricsAdapter = new MultithreadedToMetricsAdapter(_metrics);
            _cancellationTokenSource = new CancellationTokenSource();
            _startTime = DateTime.Now;
            _deadlockChecker = new DeadlockChecker();
        }

        public async void Run(IEnumerable<Philosopher> philosophers, IList<Fork> forks)
        {
            await RunAsync();
        }

        public SimulationResult GetResult() => _result;

        public async Task<SimulationResult> RunAsync()
        {
            var acquisitionManager = new ThreadSafeForkAcquisitionManager(_config.ForkAcquisitionTime);
            
            foreach (var philosopher in _philosophers)
                acquisitionManager.InitializePhilosopher(philosopher);

            // Запускаем философов в отдельных задачах
            var philosopherTasks = new List<Task>();
            for (int i = 0; i < _philosophers.Count; i++)
            {
                var philosopher = _philosophers[i];
                var leftFork = GetLeftFork(i);
                var rightFork = GetRightFork(i);

                var processor = new MultithreadedPhilosopherStateProcessor(
                    philosopher, leftFork, rightFork, _config, _strategy, acquisitionManager, _metrics);

                philosopherTasks.Add(Task.Run(() => processor.RunAsync(_cancellationTokenSource.Token)));
            }

            // Запускаем задачу для отображения состояния
            var displayTask = Task.Run(async () =>
            {
                var startTime = DateTime.Now;
                int displayCount = 0;
                
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    displayCount++;
                    var elapsed = DateTime.Now - startTime;
                    
                    // Останавливаемся если превысили время симуляции
                    if (elapsed.TotalMilliseconds >= _config.DurationSeconds * 1000)
                    {
                        _cancellationTokenSource.Cancel();
                        break;
                    }

                    // Отображаем состояние каждые DisplayInterval миллисекунд
                    _monitor.DisplayStep(displayCount, _philosophers, 
                        _forks.Select(f => new Fork(f.Id) { State = f.State, Owner = f.Owner }).ToList(), 
                        _metricsAdapter);
                    
                    // Ждем до следующего интервала отображения
                    var nextDisplayTime = displayCount * _config.DisplayInterval;
                    var currentTime = (int)elapsed.TotalMilliseconds;
                    var waitTime = Math.Max(0, nextDisplayTime - currentTime);
                    
                    if (waitTime > 0)
                    {
                        await Task.Delay(waitTime, _cancellationTokenSource.Token);
                    }
                }
            });

            // Запускаем задачу для обновления метрик
            var metricsTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _metrics.UpdateMetrics();
                    await Task.Delay(10, _cancellationTokenSource.Token);
                }
            });

            // Запускаем задачу для проверки дедлока
            var deadlockCheckTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (CheckDeadlock(_philosophers))
                    {
                        var elapsed = DateTime.Now - _startTime;
                        _result.DeadlockInfo = $"DEADLOCK detected at {elapsed.TotalMilliseconds} ms";
                        Console.WriteLine($"\n{_result.DeadlockInfo}: all philosophers hungry and each holds exactly one fork.");
                        _cancellationTokenSource.Cancel();
                        break;
                    }
                    await Task.Delay(100, _cancellationTokenSource.Token); // Проверяем каждые 100 мс
                }
            });

            // Ждем завершения симуляции
            await Task.Delay(TimeSpan.FromSeconds(_config.DurationSeconds), _cancellationTokenSource.Token);
            _cancellationTokenSource.Cancel();

            try
            {
                var allTasks = philosopherTasks.ToList();
                allTasks.Add(metricsTask);
                allTasks.Add(displayTask);
                allTasks.Add(deadlockCheckTask);
                await Task.WhenAll(allTasks);
            }
            catch (OperationCanceledException)
            {
                // Ожидаемое поведение при отмене
            }

            return FinalizeSimulation();
        }

        public bool CheckDeadlock(IList<Philosopher> philosophers) => 
            _deadlockChecker.CheckDeadlock(philosophers);

        private ThreadSafeFork GetLeftFork(int philosopherIndex)
        {
            return _forks[(philosopherIndex + _forks.Count - 1) % _forks.Count];
        }

        private ThreadSafeFork GetRightFork(int philosopherIndex)
        {
            return _forks[philosopherIndex % _forks.Count];
        }

        private SimulationResult FinalizeSimulation()
        {
            var calculator = new SimulationResultCalculator();
            return calculator.CalculateForMultithreaded(
                _metrics, 
                _philosophers, 
                _forks, 
                _config.DurationSeconds * 1000);
        }

        public MultithreadedMetricsCollector GetMetrics() => _metrics;
        public IMetricsCollector GetMetricsAdapter() => _metricsAdapter;
    }
}