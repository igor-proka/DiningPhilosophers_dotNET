using System;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation;

namespace DiningPhilosophers.Services.Simulation.Multithreaded
{
    public class MultithreadedPhilosopherStateProcessor
    {
        private readonly MultithreadedSimulationConfig _config;
        private readonly IMultithreadedMetricsCollector _metrics;
        private readonly IPhilosopherStrategy _strategy;
        private readonly ThreadSafeForkAcquisitionManager _acquisitionManager;
        private readonly Random _random;
        private readonly ThreadSafeFork _leftFork;
        private readonly ThreadSafeFork _rightFork;
        private readonly Philosopher _philosopher;

        public MultithreadedPhilosopherStateProcessor(
            Philosopher philosopher,
            ThreadSafeFork leftFork,
            ThreadSafeFork rightFork,
            MultithreadedSimulationConfig config,
            IPhilosopherStrategy strategy,
            ThreadSafeForkAcquisitionManager acquisitionManager,
            IMultithreadedMetricsCollector metrics)
            : this(philosopher, leftFork, rightFork, config, strategy, acquisitionManager, metrics, new Random())
        {
        }

        // новый конструктор — добавлен параметр random для тестовой детерминированности
        public MultithreadedPhilosopherStateProcessor(
            Philosopher philosopher,
            ThreadSafeFork leftFork,
            ThreadSafeFork rightFork,
            MultithreadedSimulationConfig config,
            IPhilosopherStrategy strategy,
            ThreadSafeForkAcquisitionManager acquisitionManager,
            IMultithreadedMetricsCollector metrics,
            Random random)
        {
            _philosopher = philosopher;
            _leftFork = leftFork;
            _rightFork = rightFork;
            _config = config;
            _strategy = strategy;
            _acquisitionManager = acquisitionManager;
            _metrics = metrics;
            _random = random ?? new Random();
        }

        public async Task RunAsync(CancellationToken ct)
        {
            // Инициализация
            _philosopher.State = PhilosopherState.Thinking;
            _philosopher.StepsRemaining = _random.Next(_config.ThinkingTimeMin, _config.ThinkingTimeMax + 1);

            while (!ct.IsCancellationRequested)
            {
                await ProcessStateAsync(ct);
            }
        }

        private async Task ProcessStateAsync(CancellationToken ct)
        {
            switch (_philosopher.State)
            {
                case PhilosopherState.Thinking:
                    await ProcessThinkingStateAsync(ct);
                    break;
                case PhilosopherState.Hungry:
                    await ProcessHungryStateAsync(ct);
                    break;
                case PhilosopherState.Eating:
                    await ProcessEatingStateAsync(ct);
                    break;
            }
        }

        public async Task ProcessThinkingStateAsync(CancellationToken ct)
        {
            await Task.Delay(_philosopher.StepsRemaining, ct);
            _philosopher.State = PhilosopherState.Hungry;
            _philosopher.CurrentAction = PhilosopherAction.None;
            _philosopher.StepsRemaining = 0;
            
            // Начинаем отсчет времени ожидания
            _metrics.StartWaiting(_philosopher.Name);
        }

        public async Task ProcessHungryStateAsync(CancellationToken ct)
        {
            // Создаем временные объекты Fork для стратегии
            var tempLeftFork = new Fork(_leftFork.Id) 
            { 
                State = _leftFork.State, 
                Owner = _leftFork.Owner 
            };
            
            var tempRightFork = new Fork(_rightFork.Id) 
            { 
                State = _rightFork.State, 
                Owner = _rightFork.Owner 
            };

            var action = _strategy.Decide(_philosopher, tempLeftFork, tempRightFork);
            _philosopher.CurrentAction = action;

            // Обрабатываем взятие вилок
            if (action.HasFlag(PhilosopherAction.TakeLeftFork) && !_philosopher.HasLeftFork)
            {
                await _acquisitionManager.TryAcquireLeftForkAsync(_philosopher, _leftFork, ct);
            }

            if (action.HasFlag(PhilosopherAction.TakeRightFork) && !_philosopher.HasRightFork)
            {
                await _acquisitionManager.TryAcquireRightForkAsync(_philosopher, _rightFork, ct);
            }

            // Обрабатываем освобождение вилок
            if (action.HasFlag(PhilosopherAction.ReleaseLeftFork) && _philosopher.HasLeftFork)
            {
                _leftFork.Release();
                _philosopher.HasLeftFork = false;
                _acquisitionManager.ResetProgress(_philosopher);
            }

            if (action.HasFlag(PhilosopherAction.ReleaseRightFork) && _philosopher.HasRightFork)
            {
                _rightFork.Release();
                _philosopher.HasRightFork = false;
                _acquisitionManager.ResetProgress(_philosopher);
            }

            // Проверяем, можем ли начать есть
            if (_philosopher.HasLeftFork && _philosopher.HasRightFork)
            {
                // Заканчиваем отсчет времени ожидания
                _metrics.StopWaiting(_philosopher.Name);
                await StartEatingAsync(ct);
                _philosopher.CurrentAction = PhilosopherAction.None;
            }
            
            // Небольшая задержка для избежания busy waiting
            // await Task.Delay(10, ct);
        }

        public async Task ProcessEatingStateAsync(CancellationToken ct)
        {
            await Task.Delay(_philosopher.StepsRemaining, ct);
            FinishEating();
        }

        private Task StartEatingAsync(CancellationToken ct)
        {
            _philosopher.State = PhilosopherState.Eating;
            _philosopher.StepsRemaining = _random.Next(_config.EatingTimeMin, _config.EatingTimeMax + 1);
            _metrics.IncrementMeal(_philosopher.Name);
            _philosopher.CurrentAction = PhilosopherAction.None;
            _acquisitionManager.ResetProgress(_philosopher);

            return Task.CompletedTask;
        }

        private void FinishEating()
        {
            if (_philosopher.HasLeftFork)
            {
                _leftFork.Release();
                _philosopher.HasLeftFork = false;
            }

            if (_philosopher.HasRightFork)
            {
                _rightFork.Release();
                _philosopher.HasRightFork = false;
            }

            _philosopher.State = PhilosopherState.Thinking;
            _philosopher.StepsRemaining = _random.Next(_config.ThinkingTimeMin, _config.ThinkingTimeMax + 1);
            _philosopher.CurrentAction = PhilosopherAction.None;
        }
    }
}