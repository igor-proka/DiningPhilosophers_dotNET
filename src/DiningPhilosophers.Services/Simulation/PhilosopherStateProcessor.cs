using System;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Simulation;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Simulation
{
    public class PhilosopherStateProcessor : IPhilosopherStateProcessor
    {
        private readonly SimulationConfig _config;
        private readonly IMetricsCollector _metrics;
        private readonly IPhilosopherStrategy _strategy;
        private readonly ICoordinator? _coordinator;
        private readonly ForkAcquisitionManager _acquisitionManager;
        private readonly Random _random;

        public PhilosopherStateProcessor(
            SimulationConfig config,
            IPhilosopherStrategy strategy,
            ICoordinator? coordinator,
            ForkAcquisitionManager acquisitionManager,
            IMetricsCollector metrics)
        {
            _config = config;
            _strategy = strategy;
            _coordinator = coordinator;
            _acquisitionManager = acquisitionManager;
            _metrics = metrics;
            _random = new Random();

            // Подписываемся на события координатора
            if (_coordinator != null)
            {
                _coordinator.DecisionEvent += OnCoordinatorDecision;
            }
        }

        private void OnCoordinatorDecision(Philosopher philosopher, PhilosopherAction action)
        {
            // Координатор решил, что философ может взять вилки
            philosopher.CurrentAction = action;
        }

        public void ProcessState(Philosopher philosopher, Fork leftFork, Fork rightFork, int step)
        {
            switch (philosopher.State)
            {
                case PhilosopherState.Thinking:
                    ProcessThinkingState(philosopher);
                    break;
                case PhilosopherState.Hungry:
                    _metrics.IncrementWaiting(philosopher.Name);
                    ProcessHungryState(philosopher, leftFork, rightFork);
                    break;
                case PhilosopherState.Eating:
                    ProcessEatingState(philosopher, leftFork, rightFork);
                    break;
            }
        }

        private void ProcessThinkingState(Philosopher philosopher)
        {
            philosopher.StepsRemaining--;
            if (philosopher.StepsRemaining <= 0)
            {
                philosopher.State = PhilosopherState.Hungry;
                philosopher.CurrentAction = PhilosopherAction.None;
                _coordinator?.NotifyHungry(philosopher);
            }
        }

        private void ProcessHungryState(Philosopher philosopher, Fork leftFork, Fork rightFork)
        {
            // Получаем действие (от координатора или стратегии)
            var action = GetAction(philosopher, leftFork, rightFork);

            // Обрабатываем действие
            ProcessForkAcquisition(philosopher, leftFork, rightFork, action);
            ProcessForkRelease(philosopher, leftFork, rightFork, action);

            // Сбрасываем действие ТОЛЬКО если начали есть
            if (philosopher.HasLeftFork && philosopher.HasRightFork)
            {
                StartEating(philosopher);
                philosopher.CurrentAction = PhilosopherAction.None; // Сброс после начала еды
            }
            // Иначе оставляем действие для следующего шага
        }
        
        private void ProcessEatingState(Philosopher philosopher, Fork leftFork, Fork rightFork)
        {
            philosopher.StepsRemaining--;
            if (philosopher.StepsRemaining <= 0)
            {
                FinishEating(philosopher, leftFork, rightFork);
                _coordinator?.NotifyFinished(philosopher);
            }
        }

        private PhilosopherAction GetAction(Philosopher philosopher, Fork leftFork, Fork rightFork)
        {
            // Если есть координатор, используем ТОЛЬКО его решение
            if (_coordinator != null)
            {
                return philosopher.CurrentAction;
            }
            
            // Иначе используем стратегию
            return _strategy.Decide(philosopher, leftFork, rightFork);
        }

        private void ProcessForkAcquisition(Philosopher philosopher, Fork leftFork, Fork rightFork, PhilosopherAction action)
        {
            if (action.HasFlag(PhilosopherAction.TakeLeftFork) && !philosopher.HasLeftFork)
            {
                _acquisitionManager.ProcessLeftForkAcquisition(philosopher, leftFork);
            }

            if (action.HasFlag(PhilosopherAction.TakeRightFork) && !philosopher.HasRightFork)
            {
                _acquisitionManager.ProcessRightForkAcquisition(philosopher, rightFork);
            }
        }

        private void ProcessForkRelease(Philosopher philosopher, Fork leftFork, Fork rightFork, PhilosopherAction action)
        {
            if (action.HasFlag(PhilosopherAction.ReleaseLeftFork) && philosopher.HasLeftFork)
            {
                leftFork.State = ForkState.Available;
                leftFork.Owner = null;
                philosopher.HasLeftFork = false;
                _acquisitionManager.ResetProgress(philosopher);
            }

            if (action.HasFlag(PhilosopherAction.ReleaseRightFork) && philosopher.HasRightFork)
            {
                rightFork.State = ForkState.Available;
                rightFork.Owner = null;
                philosopher.HasRightFork = false;
                _acquisitionManager.ResetProgress(philosopher);
            }
        }

        private void StartEating(Philosopher philosopher)
        {
            philosopher.State = PhilosopherState.Eating;
            philosopher.StepsRemaining = _random.Next(_config.EatingTimeMin, _config.EatingTimeMax + 1);
            _metrics.IncrementMeal(philosopher.Name);
            philosopher.CurrentAction = PhilosopherAction.None;
            _acquisitionManager.ResetProgress(philosopher);
        }

        private void FinishEating(Philosopher philosopher, Fork leftFork, Fork rightFork)
        {
            if (philosopher.HasLeftFork)
            {
                leftFork.State = ForkState.Available;
                leftFork.Owner = null;
                philosopher.HasLeftFork = false;
            }

            if (philosopher.HasRightFork)
            {
                rightFork.State = ForkState.Available;
                rightFork.Owner = null;
                philosopher.HasRightFork = false;
            }

            philosopher.State = PhilosopherState.Thinking;
            philosopher.StepsRemaining = _random.Next(_config.ThinkingTimeMin, _config.ThinkingTimeMax + 1);
            philosopher.CurrentAction = PhilosopherAction.None;
        }
    }
}