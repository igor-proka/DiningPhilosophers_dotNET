using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation;
using Moq;
using Xunit;

namespace DiningPhilosophers.Tests.Simulation
{
    // Тесты для пошагового обработчика состояний философа.
    // Проверяет логику переходов между состояниями в синхронном режиме.
    public class PhilosopherStateProcessorTests
    {
        private readonly Mock<IPhilosopherStrategy> _strategyMock = new Mock<IPhilosopherStrategy>();
        private readonly Mock<ICoordinator> _coordinatorMock = new Mock<ICoordinator>();
        private readonly Mock<IMetricsCollector> _metricsMock = new Mock<IMetricsCollector>();
        private readonly ForkAcquisitionManager _acquisitionManager;
        private readonly SimulationConfig _config;

        public PhilosopherStateProcessorTests()
        {
            // Конфигурация для тестов с фиксированными значениями для предсказуемости
            _config = new SimulationConfig
            {
                ThinkingTimeMin = 3,
                ThinkingTimeMax = 3,     // Фиксируем для тестов
                EatingTimeMin = 4,
                EatingTimeMax = 4,       // Фиксируем для тестов
                ForkAcquisitionTime = 2, // 2 шага на взятие вилки
                TotalSteps = 1000,
                DisplayInterval = 100,
                UseCoordinator = false
            };

            _acquisitionManager = new ForkAcquisitionManager(_config.ForkAcquisitionTime);
        }

        // Тест: Проверяет полный жизненный цикл философа Thinking → Hungry → Eating → Thinking.
        // Что проверяем:
        // 1. Начинаем с Thinking с минимальным временем
        // 2. Переходим в Hungry после истечения времени
        // 3. Берем вилки и переходим в Eating
        // 4. Заканчиваем есть и возвращаемся в Thinking
        // 5. Весь цикл завершается корректно
        [Fact]
        public void ProcessState_FullLifecycle_ThinkingHungryEatingThinking()
        {
            // Arrange - готовим философа на старте цикла
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Thinking,
                StepsRemaining = 1 // Минимальное время для быстрого перехода
            };
            
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };
            
            _acquisitionManager.InitializePhilosopher(philosopher);
            
            // Настраиваем стратегию для полного цикла
            _strategyMock.Setup(s => s.Decide(It.IsAny<Philosopher>(), It.IsAny<Fork>(), It.IsAny<Fork>()))
                .Returns<Philosopher, Fork, Fork>((p, left, right) =>
                {
                    // При первом вызове в Hungry - берем вилки
                    if (p.State == PhilosopherState.Hungry && !p.HasLeftFork && !p.HasRightFork)
                        return PhilosopherAction.TakeLeftFork | PhilosopherAction.TakeRightFork;
                    
                    return PhilosopherAction.None;
                });
            
            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, coordinator: null, 
                _acquisitionManager, _metricsMock.Object);
            
            // Act & Assert - выполняем полный цикл шаг за шагом
            
            // ШАГ 1: Thinking → Hungry (завершаем размышления)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            Assert.Equal(PhilosopherState.Hungry, philosopher.State);
            Assert.Equal(0, philosopher.StepsRemaining);
            
            // ШАГ 2: Начинаем брать вилки (прогресс 1)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);
            Assert.Equal(PhilosopherState.Hungry, philosopher.State);
            Assert.False(philosopher.HasLeftFork); // Еще не взял
            Assert.False(philosopher.HasRightFork);
            
            // ШАГ 3: Завершаем взятие вилок (прогресс 2) и переходим в Eating
            processor.ProcessState(philosopher, leftFork, rightFork, step: 3);
            Assert.Equal(PhilosopherState.Eating, philosopher.State);
            Assert.True(philosopher.HasLeftFork);
            Assert.True(philosopher.HasRightFork);
            Assert.Equal(_config.EatingTimeMax, philosopher.StepsRemaining);
            
            // Проверяем, что вилки заняты этим философом
            Assert.Equal(ForkState.InUse, leftFork.State);
            Assert.Equal(ForkState.InUse, rightFork.State);
            Assert.Equal("ТестовыйФилософ", leftFork.Owner);
            Assert.Equal("ТестовыйФилософ", rightFork.Owner);
            
            // ШАГ 4-7: Продолжаем есть (уменьшаем StepsRemaining)
            for (int step = 4; step <= 6; step++)
            {
                processor.ProcessState(philosopher, leftFork, rightFork, step);
                Assert.Equal(PhilosopherState.Eating, philosopher.State);
                Assert.True(philosopher.HasLeftFork);
                Assert.True(philosopher.HasRightFork);
            }
            
            // ШАГ 8: Последний шаг еды → Thinking
            processor.ProcessState(philosopher, leftFork, rightFork, step: 7);
            Assert.Equal(PhilosopherState.Thinking, philosopher.State);
            
            // Проверяем, что вилки освобождены
            Assert.False(philosopher.HasLeftFork);
            Assert.False(philosopher.HasRightFork);
            Assert.Equal(ForkState.Available, leftFork.State);
            Assert.Equal(ForkState.Available, rightFork.State);
            Assert.Null(leftFork.Owner);
            Assert.Null(rightFork.Owner);
            
            // Проверяем, что установлено новое время размышлений
            Assert.InRange(philosopher.StepsRemaining, 
                _config.ThinkingTimeMin, _config.ThinkingTimeMax);
            
            // Проверяем метрики
            _metricsMock.Verify(m => m.IncrementWaiting("ТестовыйФилософ"), Times.AtLeast(2));
            _metricsMock.Verify(m => m.IncrementMeal("ТестовыйФилософ"), Times.Once());
            
            // Убеждаемся, что цикл завершился корректно
            Assert.Equal(PhilosopherState.Thinking, philosopher.State);
            Assert.True(philosopher.StepsRemaining > 0);
        }
        
        // Тест: Проверяет переход из Thinking в Hungry.
        // Что проверяем:
        // 1. Уменьшение StepsRemaining при каждом вызове ProcessState
        // 2. Переход в Hungry при достижении нуля
        // 3. Уведомление координатора о голоде
        // 4. Сброс CurrentAction
        [Fact]
        public void ProcessState_ThinkingToHungry_WhenStepsRemainingZero()
        {
            // Arrange - подготовка тестовых данных
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Thinking, 
                StepsRemaining = 1  // Остался 1 шаг до голода
            };
            
            var leftFork = new Fork(1);
            var rightFork = new Fork(2);
            _acquisitionManager.InitializePhilosopher(philosopher);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, _coordinatorMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обработка состояния (шаг должен обнулить StepsRemaining)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);

            // Assert - проверка результатов
            Assert.Equal(PhilosopherState.Hungry, philosopher.State);
            Assert.Equal(0, philosopher.StepsRemaining);
            Assert.Equal(PhilosopherAction.None, philosopher.CurrentAction);
            
            // Координатор должен быть уведомлен о голоде философа
            _coordinatorMock.Verify(c => c.NotifyHungry(philosopher), Times.Once);
        }

        // Тест: Проверяет, что философ продолжает думать, если время не истекло.
        // Что проверяем:
        // 1. StepsRemaining уменьшается на 1 за шаг
        // 2. Состояние остается Thinking
        // 3. Координатор не уведомляется
        [Fact]
        public void ProcessState_RemainsThinking_WhenStepsRemainingPositive()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Thinking, 
                StepsRemaining = 3  // Еще 3 шага до голода
            };
            
            var leftFork = new Fork(1);
            var rightFork = new Fork(2);
            _acquisitionManager.InitializePhilosopher(philosopher);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, _coordinatorMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем 2 шага (должно остаться 1)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);

            // Assert
            Assert.Equal(PhilosopherState.Thinking, philosopher.State);
            Assert.Equal(1, philosopher.StepsRemaining); // 3 - 2 = 1
            _coordinatorMock.Verify(c => c.NotifyHungry(It.IsAny<Philosopher>()), Times.Never);
        }

        // Тест: Проверяет полный процесс взятия вилок и перехода в Eating.
        // Что проверяем:
        // 1. Философ получает решение от стратегии (без координатора)
        // 2. Постепенное взятие вилок через ForkAcquisitionManager
        // 3. Переход в Eating при наличии обеих вилок
        // 4. Установка времени еды и обновление метрик
        [Fact]
        public void ProcessState_HungryToEating_WhenBothForksAcquired()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry 
            };
            
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            // Стратегия разрешает взять обе вилки
            _strategyMock.Setup(s => s.Decide(philosopher, leftFork, rightFork))
                .Returns(PhilosopherAction.TakeLeftFork | PhilosopherAction.TakeRightFork);

            // Создаем процессор БЕЗ координатора (используем только стратегию)
            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, coordinator: null, 
                _acquisitionManager, _metricsMock.Object);

            // Act - симулируем процесс взятия вилок
            // Шаг 1: Начинаем взятие (прогресс = 1)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            
            // Шаг 2: Завершаем взятие (прогресс = 2, достигнут ForkAcquisitionTime)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);

            // Assert - проверяем результаты
            Assert.True(philosopher.HasLeftFork, "Должен иметь левую вилку");
            Assert.True(philosopher.HasRightFork, "Должен иметь правую вилку");
            Assert.Equal(PhilosopherState.Eating, philosopher.State);
            Assert.Equal(ForkState.InUse, leftFork.State);
            Assert.Equal(ForkState.InUse, rightFork.State);
            Assert.Equal("ТестовыйФилософ", leftFork.Owner);
            Assert.Equal("ТестовыйФилософ", rightFork.Owner);
            
            // Проверяем, что установлено корректное время еды
            Assert.Equal(_config.EatingTimeMax, philosopher.StepsRemaining);
            
            // Проверяем обновление метрик
            _metricsMock.Verify(m => m.IncrementMeal("ТестовыйФилософ"), Times.Once);
            _metricsMock.Verify(m => m.IncrementWaiting("ТестовыйФилософ"), Times.Exactly(2));
        }

        // Тест: Проверяет поведение при использовании координатора.
        // Что проверяем:
        // 1. Философ использует решение координатора, а не стратегии
        // 2. Координатор отправляет событие с действием
        // 3. Философ выполняет действие от координатора
        [Fact]
        public void ProcessState_UsesCoordinatorDecision_WhenCoordinatorPresent()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry,
                CurrentAction = PhilosopherAction.TakeLeftFork  // Координатор уже дал команду
            };
            
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            // Координатор уже дал команду через событие
            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, _coordinatorMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем 2 шага для взятия вилки
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);

            // Assert
            // Философ должен взять левую вилку по команде координатора
            Assert.True(philosopher.HasLeftFork);
            Assert.Equal(ForkState.InUse, leftFork.State);
            Assert.Equal("ТестовыйФилософ", leftFork.Owner);
            
            // Стратегия не должна вызываться при наличии координатора
            _strategyMock.Verify(
                s => s.Decide(It.IsAny<Philosopher>(), It.IsAny<Fork>(), It.IsAny<Fork>()), 
                Times.Never);
        }

        // Тест: Проверяет освобождение вилок по команде стратегии.
        // Что проверяем:
        // 1. Стратегия может приказать освободить вилки
        // 2. Вилки мгновенно освобождаются
        // 3. Прогресс взятия сбрасывается
        // 4. Состояние вилок возвращается в Available
        [Fact]
        public void ProcessState_ReleasesForks_WhenStrategyDecides()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry,
                HasLeftFork = true,
                HasRightFork = true
            };
            
            var leftFork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "ТестовыйФилософ" 
            };
            
            var rightFork = new Fork(2) 
            { 
                State = ForkState.InUse, 
                Owner = "ТестовыйФилософ" 
            };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            // Стратегия приказывает освободить обе вилки
            _strategyMock.Setup(s => s.Decide(philosopher, leftFork, rightFork))
                .Returns(PhilosopherAction.ReleaseLeftFork | PhilosopherAction.ReleaseRightFork);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, coordinator: null, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем состояние (освобождение должно быть мгновенным)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);

            // Assert
            Assert.False(philosopher.HasLeftFork, "Левая вилка должна быть освобождена");
            Assert.False(philosopher.HasRightFork, "Правая вилка должна быть освобождена");
            Assert.Equal(ForkState.Available, leftFork.State);
            Assert.Equal(ForkState.Available, rightFork.State);
            Assert.Null(leftFork.Owner);
            Assert.Null(rightFork.Owner);
        }

        // Тест: Проверяет, что философ остается голодным, если вилки заняты.
        // Что проверяем:
        // 1. Стратегия хочет взять вилки
        // 2. Вилки заняты другими философами
        // 3. Философ не может взять вилки
        // 4. Остается в состоянии Hungry
        [Fact]
        public void ProcessState_RemainsHungry_WhenForksUnavailable()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry 
            };
            
            var leftFork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "ДругойФилософ" 
            };
            
            var rightFork = new Fork(2) 
            { 
                State = ForkState.InUse, 
                Owner = "ДругойФилософ" 
            };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            // Стратегия хочет взять вилки, но они заняты
            _strategyMock.Setup(s => s.Decide(philosopher, leftFork, rightFork))
                .Returns(PhilosopherAction.TakeLeftFork | PhilosopherAction.TakeRightFork);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, coordinator: null, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем несколько шагов
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);
            processor.ProcessState(philosopher, leftFork, rightFork, step: 3);

            // Assert
            Assert.False(philosopher.HasLeftFork, "Не должен иметь левую вилку");
            Assert.False(philosopher.HasRightFork, "Не должен иметь правую вилку");
            Assert.Equal(PhilosopherState.Hungry, philosopher.State);
            
            // Вилки остаются у других владельцев
            Assert.Equal("ДругойФилософ", leftFork.Owner);
            Assert.Equal("ДругойФилософ", rightFork.Owner);
        }

        // Тест: Проверяет завершение еды и возврат в состояние Thinking.
        // Что проверяем:
        // 1. Уменьшение StepsRemaining во время еды
        // 2. Возврат вилок при завершении еды
        // 3. Переход в Thinking с новым временем размышлений
        // 4. Уведомление координатора о завершении
        [Fact]
        public void ProcessState_EatingToThinking_WhenStepsRemainingZero()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Eating, 
                StepsRemaining = 1,  // Последний шаг еды
                HasLeftFork = true, 
                HasRightFork = true 
            };
            
            var leftFork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "ТестовыйФилософ" 
            };
            
            var rightFork = new Fork(2) 
            { 
                State = ForkState.InUse, 
                Owner = "ТестовыйФилософ" 
            };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, _coordinatorMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем последний шаг еды
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);

            // Assert
            Assert.Equal(PhilosopherState.Thinking, philosopher.State);
            Assert.Equal(_config.ThinkingTimeMax, philosopher.StepsRemaining); // Новое время размышлений
            
            // Вилки должны быть освобождены
            Assert.False(philosopher.HasLeftFork);
            Assert.False(philosopher.HasRightFork);
            Assert.Equal(ForkState.Available, leftFork.State);
            Assert.Equal(ForkState.Available, rightFork.State);
            Assert.Null(leftFork.Owner);
            Assert.Null(rightFork.Owner);
            
            // Координатор должен быть уведомлен
            _coordinatorMock.Verify(c => c.NotifyFinished(philosopher), Times.Once);
        }

        // Тест: Проверяет, что философ продолжает есть, если время не истекло.
        // Что проверяем:
        // 1. StepsRemaining уменьшается на 1 за шаг
        // 2. Состояние остается Eating
        // 3. Вилки остаются у философа
        // 4. Координатор не уведомляется
        [Fact]
        public void ProcessState_ContinuesEating_WhenStepsRemainingPositive()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Eating, 
                StepsRemaining = 3,  // Еще 3 шага еды
                HasLeftFork = true, 
                HasRightFork = true 
            };
            
            var leftFork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "ТестовыйФилософ" 
            };
            
            var rightFork = new Fork(2) 
            { 
                State = ForkState.InUse, 
                Owner = "ТестовыйФилософ" 
            };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, _coordinatorMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем 2 шага (должно остаться 1)
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);

            // Assert
            Assert.Equal(PhilosopherState.Eating, philosopher.State);
            Assert.Equal(1, philosopher.StepsRemaining); // 3 - 2 = 1
            
            // Вилки остаются у философа
            Assert.True(philosopher.HasLeftFork);
            Assert.True(philosopher.HasRightFork);
            Assert.Equal("ТестовыйФилософ", leftFork.Owner);
            Assert.Equal("ТестовыйФилософ", rightFork.Owner);
            
            // Координатор еще не уведомляется
            _coordinatorMock.Verify(c => c.NotifyFinished(It.IsAny<Philosopher>()), Times.Never);
        }

        // Тест: Проверяет взаимодействие с ForkAcquisitionManager.
        // Что проверяем:
        // 1. Постепенный прогресс взятия вилки
        // 2. Сброс прогресса при освобождении вилки
        // 3. Финализация взятия при достижении ForkAcquisitionTime
        [Fact]
        public void ProcessState_ManagesForkAcquisitionProgress_Correctly()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry 
            };
            
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            // Стратегия берет только левую вилку
            _strategyMock.Setup(s => s.Decide(philosopher, leftFork, rightFork))
                .Returns(PhilosopherAction.TakeLeftFork);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, coordinator: null, 
                _acquisitionManager, _metricsMock.Object);

            // Act & Assert - проверяем прогресс взятия
            
            // Шаг 1: прогресс = 1, вилка еще не взята
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            Assert.False(philosopher.HasLeftFork);
            Assert.Equal(ForkState.Available, leftFork.State);
            
            // Шаг 2: прогресс = 2 (достигнут ForkAcquisitionTime), вилка должна быть взята
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);
            Assert.True(philosopher.HasLeftFork);
            Assert.Equal(ForkState.InUse, leftFork.State);
            Assert.Equal("ТестовыйФилософ", leftFork.Owner);
        }

        // Тест: Проверяет, что метрики ожидания корректно инкрементируются.
        // Что проверяем:
        // 1. IncrementWaiting вызывается на каждом шаге в состоянии Hungry
        // 2. Не вызывается в состояниях Thinking и Eating
        [Fact]
        public void ProcessState_IncrementsWaitingMetrics_OnlyWhenHungry()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry 
            };
            
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };
            
            _acquisitionManager.InitializePhilosopher(philosopher);

            _strategyMock.Setup(s => s.Decide(philosopher, leftFork, rightFork))
                .Returns(PhilosopherAction.None);

            var processor = new PhilosopherStateProcessor(
                _config, _strategyMock.Object, coordinator: null, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем 3 шага в состоянии Hungry
            processor.ProcessState(philosopher, leftFork, rightFork, step: 1);
            processor.ProcessState(philosopher, leftFork, rightFork, step: 2);
            processor.ProcessState(philosopher, leftFork, rightFork, step: 3);

            // Assert
            // Метрика ожидания должна вызываться 3 раза (по разу на каждый шаг в Hungry)
            _metricsMock.Verify(m => m.IncrementWaiting("ТестовыйФилософ"), Times.Exactly(3));
            
            // Проверяем, что не вызывается для других состояний
            philosopher.State = PhilosopherState.Thinking;
            processor.ProcessState(philosopher, leftFork, rightFork, step: 4);
            _metricsMock.Verify(m => m.IncrementWaiting("ТестовыйФилософ"), Times.Exactly(3)); // Не изменилось
        }
    }
}