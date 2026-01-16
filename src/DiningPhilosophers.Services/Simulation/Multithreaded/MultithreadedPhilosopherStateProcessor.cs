using System;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation;
using DiningPhilosophers.Persistence;
using DiningPhilosophers.Persistence.Entities;

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
        public string Name => _philosopher?.Name ?? "UNKNOWN";

        // Для persistence
        private ISimulationPersistence? _persistence;
        private Guid _runId;
        private PhilosopherState? _previousPhilosopherState;
        private int _previousStepsRemaining;
        private bool _previousHasLeftFork;
        private bool _previousHasRightFork;
        private PhilosopherAction _previousCurrentAction;
        private ForkState? _previousLeftForkState;
        private ForkState? _previousRightForkState;
        private string? _previousLeftOwner;
        private string? _previousRightOwner;

        // Static seed для уникальных Random экземпляров среди философов
        private static int _seed = Environment.TickCount;

        public Philosopher Philosopher { get; }
        public ThreadSafeFork LeftFork { get; }
        public ThreadSafeFork RightFork { get; }
        public IMultithreadedMetricsCollector Metrics { get; }

        public MultithreadedPhilosopherStateProcessor(
            Philosopher philosopher, ThreadSafeFork leftFork, ThreadSafeFork rightFork,
            MultithreadedSimulationConfig config, IPhilosopherStrategy strategy,
            ThreadSafeForkAcquisitionManager acquisitionManager, IMultithreadedMetricsCollector metrics)
        {
            Philosopher = philosopher ?? throw new ArgumentNullException(nameof(philosopher));
            LeftFork = leftFork ?? throw new ArgumentNullException(nameof(leftFork));
            RightFork = rightFork ?? throw new ArgumentNullException(nameof(rightFork));
            Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            
            _philosopher = philosopher;
            _leftFork = leftFork;
            _rightFork = rightFork;
            _config = config;
            _strategy = strategy;
            _acquisitionManager = acquisitionManager;
            _metrics = metrics;

            // Уникальное случайное начальное значение для этого экземпляра во избежание синхронизации
            _random = new Random(Interlocked.Increment(ref _seed));

            // Инициализация начальных состояний философов
            _philosopher.State = PhilosopherState.Thinking;
            _philosopher.StepsRemaining = _random.Next(_config.ThinkingTimeMin, _config.ThinkingTimeMax + 1);
            Console.WriteLine($"Initialized { _philosopher.Name } with initial steps: { _philosopher.StepsRemaining }");

            // Инициализация предыдущих состояний для обнаружения изменений
            UpdatePreviousStates();
        }

        // Новый конструктор (для тестов) — добавлен параметр random для тестовой детерминированности
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
            Philosopher = philosopher ?? throw new ArgumentNullException(nameof(philosopher));
            LeftFork = leftFork ?? throw new ArgumentNullException(nameof(leftFork));
            RightFork = rightFork ?? throw new ArgumentNullException(nameof(rightFork));
            Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

            _philosopher = philosopher;
            _leftFork = leftFork;
            _rightFork = rightFork;
            _config = config;
            _strategy = strategy;
            _acquisitionManager = acquisitionManager;
            _metrics = metrics;
            _random = random ?? new Random();
        }

        public void SetPersistence(ISimulationPersistence persistence, RunContext runContext)
        {
            _persistence = persistence;
            _runId = runContext.RunId;
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
            while (!ct.IsCancellationRequested)
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

                // Записываем, если есть какие-то изменения
                await LogIfChangedAsync();

                // Небольшая задержка для избежания busy looping
                // await Task.Delay(1, ct);
            }
        }

        public async Task ProcessThinkingStateAsync(CancellationToken ct)
        {
            if (!ct.IsCancellationRequested)
            {
                await Task.Delay(_philosopher.StepsRemaining, ct);

                _philosopher.State = PhilosopherState.Hungry;
                _philosopher.CurrentAction = PhilosopherAction.None;

                _philosopher.StepsRemaining = 0;

                // Начинаем отсчет времени ожидания
                _metrics.StartWaiting(_philosopher.Name);
            }
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
            if (!ct.IsCancellationRequested)
            {
                await Task.Delay(_philosopher.StepsRemaining, ct);
                FinishEating();
            }
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

        private async Task LogIfChangedAsync()
        {
            if (_persistence == null) return;

            bool philosopherChanged = _philosopher.State != _previousPhilosopherState ||
                                      _philosopher.StepsRemaining != _previousStepsRemaining ||
                                      _philosopher.HasLeftFork != _previousHasLeftFork ||
                                      _philosopher.HasRightFork != _previousHasRightFork ||
                                      _philosopher.CurrentAction != _previousCurrentAction;

            bool leftForkChanged = _leftFork.State != _previousLeftForkState || _leftFork.Owner != _previousLeftOwner;
            bool rightForkChanged = _rightFork.State != _previousRightForkState || _rightFork.Owner != _previousRightOwner;

            if (philosopherChanged)
            {
                await _persistence.LogPhilosopherEventAsync(_runId, new PhilosopherStateEvent
                {
                    PhilosopherName = _philosopher.Name,
                    State = _philosopher.State.ToString(),
                    StepsRemaining = _philosopher.StepsRemaining,
                    HasLeftFork = _philosopher.HasLeftFork,
                    HasRightFork = _philosopher.HasRightFork,
                    CurrentAction = _philosopher.CurrentAction.ToString()
                });
            }

            if (leftForkChanged)
            {
                await _persistence.LogForkEventAsync(_runId, new ForkStateEvent
                {
                    ForkNumber = _leftFork.Id,
                    State = _leftFork.State.ToString(),
                    Owner = _leftFork.Owner
                });
            }

            if (rightForkChanged)
            {
                await _persistence.LogForkEventAsync(_runId, new ForkStateEvent
                {
                    ForkNumber = _rightFork.Id,
                    State = _rightFork.State.ToString(),
                    Owner = _rightFork.Owner
                });
            }

            UpdatePreviousStates();
        }

        private void UpdatePreviousStates()
        {
            _previousPhilosopherState = _philosopher.State;
            _previousStepsRemaining = _philosopher.StepsRemaining;
            _previousHasLeftFork = _philosopher.HasLeftFork;
            _previousHasRightFork = _philosopher.HasRightFork;
            _previousCurrentAction = _philosopher.CurrentAction;
            _previousLeftForkState = _leftFork.State;
            _previousRightForkState = _rightFork.State;
            _previousLeftOwner = _leftFork.Owner;
            _previousRightOwner = _rightFork.Owner;
        }
    }
}