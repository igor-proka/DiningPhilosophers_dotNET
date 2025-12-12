using System;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using Moq;
using Xunit;

namespace DiningPhilosophers.Tests.Simulation
{
    public class MultithreadedPhilosopherStateProcessorTests
    {
        private readonly Mock<IPhilosopherStrategy> _strategyMock = new Mock<IPhilosopherStrategy>();
        private readonly Mock<IMultithreadedMetricsCollector> _metricsMock = new Mock<IMultithreadedMetricsCollector>();
        private readonly ThreadSafeForkAcquisitionManager _acquisitionManager;
        private readonly MultithreadedSimulationConfig _config;
        private readonly Random _testRandom = new Random(123); // Фиксированный seed для воспроизводимости

        public MultithreadedPhilosopherStateProcessorTests()
        {
            // Конфигурация для тестов с предсказуемыми временами
            _config = new MultithreadedSimulationConfig
            {
                ThinkingTimeMin = 5,
                ThinkingTimeMax = 5,    // Фиксированное время для тестов
                EatingTimeMin = 5,
                EatingTimeMax = 5,      // Фиксированное время для тестов
                ForkAcquisitionTime = 3,
                DisplayInterval = 1,
                DurationSeconds = 60    // Для полноты конфигурации
            };

            _acquisitionManager = new ThreadSafeForkAcquisitionManager(_config.ForkAcquisitionTime);
        }

        // Тест: Проверяет полный цикл жизни философа
        // Thinking → Hungry → Eating → Thinking
        // Что проверяем:
        // 1. Философ начинает с состояния Thinking
        // 2. После окончания времени размышлений переходит в Hungry
        // 3. При наличии вилок переходит в Eating
        // 4. После окончания еды возвращается в Thinking
        [Fact]
        public async Task RunAsync_TransitionsThroughStates_FullLifecycle()
        {
            // Arrange - подготовка тестовых данных
            var philosopher = new Philosopher("ТестовыйФилософ");
            var leftFork = new ThreadSafeFork(1);
            var rightFork = new ThreadSafeFork(2);

            // Настраиваем стратегию: всегда разрешаем брать обе вилки
            _strategyMock
                .Setup(s => s.Decide(It.IsAny<Philosopher>(), It.IsAny<Fork>(), It.IsAny<Fork>()))
                .Returns(PhilosopherAction.TakeLeftFork | PhilosopherAction.TakeRightFork);

            // Отключаем метрики для чистоты теста
            _metricsMock.Setup(m => m.StartWaiting(It.IsAny<string>()));
            _metricsMock.Setup(m => m.StopWaiting(It.IsAny<string>()));
            _metricsMock.Setup(m => m.IncrementMeal(It.IsAny<string>()));

            var processor = new MultithreadedPhilosopherStateProcessor(
                philosopher,
                leftFork,
                rightFork,
                _config,
                _strategyMock.Object,
                _acquisitionManager,
                _metricsMock.Object);

            var cts = new CancellationTokenSource();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Act - запуск обработчика в фоновой задаче
            var runTask = processor.RunAsync(cts.Token);

            try
            {
                // Шаг 1: Проверяем переход Thinking → Hungry
                await WaitUntilAsync(() => philosopher.State == PhilosopherState.Hungry, timeoutCts.Token);
                
                // Шаг 2: Проверяем переход Hungry → Eating (при наличии вилок)
                await WaitUntilAsync(() => philosopher.State == PhilosopherState.Eating, timeoutCts.Token);
                
                // Шаг 3: Проверяем переход Eating → Thinking (после еды)
                await WaitUntilAsync(() => philosopher.State == PhilosopherState.Thinking, timeoutCts.Token);

                // Проверяем, что вилки были освобождены после еды
                Assert.False(philosopher.HasLeftFork);
                Assert.False(philosopher.HasRightFork);
                Assert.Equal(ForkState.Available, leftFork.State);
                Assert.Equal(ForkState.Available, rightFork.State);

                // Проверяем вызовы метрик
                _metricsMock.Verify(m => m.StartWaiting("ТестовыйФилософ"), Times.AtLeastOnce());
                _metricsMock.Verify(m => m.StopWaiting("ТестовыйФилософ"), Times.AtLeastOnce());
                _metricsMock.Verify(m => m.IncrementMeal("ТестовыйФилософ"), Times.AtLeastOnce());
            }
            finally
            {
                // Останавливаем процессор
                cts.Cancel();
                try 
                {await runTask; } catch (TaskCanceledException) {}
            }
        }

        // Тест: Проверяет процесс взятия вилок в состоянии Hungry
        // Что проверяем:
        // 1. Философ получает решение от стратегии
        // 2. Пытается взять вилки через менеджер приобретения
        // 3. При успешном взятии обеих вилок начинает есть
        // 4. Метрики корректно обновляются
        [Fact]
        public async Task ProcessHungryState_AcquiresForksAndStartsEating_WhenForksAvailable()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry 
            };
            
            var leftFork = new ThreadSafeFork(1);
            var rightFork = new ThreadSafeFork(2);
            
            var cts = new CancellationTokenSource();

            // Стратегия разрешает взять обе вилки
            _strategyMock.Setup(s => s.Decide(It.IsAny<Philosopher>(), It.IsAny<Fork>(), It.IsAny<Fork>()))
                .Returns(PhilosopherAction.TakeLeftFork | PhilosopherAction.TakeRightFork);

            var processor = new MultithreadedPhilosopherStateProcessor(
                philosopher, leftFork, rightFork, _config, _strategyMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act - обрабатываем состояние голода
            await processor.ProcessHungryStateAsync(cts.Token);

            // Assert - проверяем результаты
            // Даем время на взятие вилок (время приобретения + запас)
            await Task.Delay(_config.ForkAcquisitionTime + 20);
            
            // Проверяем, что философ взял обе вилки
            Assert.True(philosopher.HasLeftFork, "Философ должен иметь левую вилку");
            Assert.True(philosopher.HasRightFork, "Философ должен иметь правую вилку");
            
            // Проверяем переход в состояние Eating
            Assert.Equal(PhilosopherState.Eating, philosopher.State);
            
            // Проверяем, что вилки помечены как используемые
            Assert.Equal(ForkState.InUse, leftFork.State);
            Assert.Equal(ForkState.InUse, rightFork.State);
            Assert.Equal("ТестовыйФилософ", leftFork.Owner);
            Assert.Equal("ТестовыйФилософ", rightFork.Owner);
            
            // Проверяем вызовы метрик
            _metricsMock.Verify(m => m.StopWaiting("ТестовыйФилософ"), Times.Once(),
                "Должно быть зафиксировано окончание ожидания");
            _metricsMock.Verify(m => m.IncrementMeal("ТестовыйФилософ"), Times.Once(),
                "Должна быть зафиксирована новая трапеза");
        }

        // Тест: Проверяет поведение при недоступности вилок
        // Что проверяем:
        // 1. Если вилки заняты, философ остается в состоянии Hungry
        // 2. Не происходит перехода в Eating
        // 3. Метрики ожидания продолжают накапливаться
        [Fact]
        public async Task ProcessHungryState_RemainsHungry_WhenForksUnavailable()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry 
            };
            
            var leftFork = new ThreadSafeFork(1);
            var rightFork = new ThreadSafeFork(2);
            
            // Занимаем вилки другим философом
            leftFork.TryAcquire("ДругойФилософ");
            rightFork.TryAcquire("ДругойФилософ");
            
            var cts = new CancellationTokenSource();

            // Стратегия хочет взять вилки, но они заняты
            _strategyMock.Setup(s => s.Decide(It.IsAny<Philosopher>(), It.IsAny<Fork>(), It.IsAny<Fork>()))
                .Returns(PhilosopherAction.TakeLeftFork | PhilosopherAction.TakeRightFork);

            var processor = new MultithreadedPhilosopherStateProcessor(
                philosopher, leftFork, rightFork, _config, _strategyMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act
            await processor.ProcessHungryStateAsync(cts.Token);

            // Assert
            await Task.Delay(50); // Даем время на попытку взятия
            
            // Философ должен остаться голодным без вилок
            Assert.False(philosopher.HasLeftFork, "Не должен иметь левую вилку");
            Assert.False(philosopher.HasRightFork, "Не должен иметь правую вилку");
            Assert.Equal(PhilosopherState.Hungry, philosopher.State);
            
            // Метрика остановки ожидания не должна вызываться
            _metricsMock.Verify(m => m.StopWaiting("ТестовыйФилософ"), Times.Never());
        }

        // Тест: Проверяет процесс освобождения вилок по команде стратегии
        // Что проверяем:
        // 1. Философ освобождает вилки по команде Release
        // 2. Состояние вилок возвращается в Available
        // 3. Прогресс приобретения сбрасывается
        [Fact]
        public async Task ProcessHungryState_ReleasesForks_WhenStrategyDecides()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Hungry,
                HasLeftFork = true,
                HasRightFork = true
            };
            
            var leftFork = new ThreadSafeFork(1);
            var rightFork = new ThreadSafeFork(2);
            
            // Философ уже "держит" вилки
            leftFork.TryAcquire("ТестовыйФилософ");
            rightFork.TryAcquire("ТестовыйФилософ");
            
            var cts = new CancellationTokenSource();

            // Стратегия приказывает освободить вилки
            _strategyMock.Setup(s => s.Decide(It.IsAny<Philosopher>(), It.IsAny<Fork>(), It.IsAny<Fork>()))
                .Returns(PhilosopherAction.ReleaseLeftFork | PhilosopherAction.ReleaseRightFork);

            var processor = new MultithreadedPhilosopherStateProcessor(
                philosopher, leftFork, rightFork, _config, _strategyMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            // Act
            await processor.ProcessHungryStateAsync(cts.Token);

            // Assert
            // Освобождение должно быть мгновенным
            Assert.False(philosopher.HasLeftFork, "Левая вилка должна быть освобождена");
            Assert.False(philosopher.HasRightFork, "Правая вилка должна быть освобождена");
            Assert.Equal(ForkState.Available, leftFork.State);
            Assert.Equal(ForkState.Available, rightFork.State);
            Assert.Null(leftFork.Owner);
            Assert.Null(rightFork.Owner);
        }

        // Тест: Проверяет обработку состояния Eating
        // Что проверяем:
        // 1. После окончания времени еды философ возвращает вилки
        // 2. Переходит в состояние Thinking
        // 3. Устанавливает новое время размышлений
        [Fact]
        public async Task ProcessEatingState_FinishesEatingAndReleasesForks()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Eating,
                StepsRemaining = 5, // Короткое время еды для теста
                HasLeftFork = true,
                HasRightFork = true
            };
            
            var leftFork = new ThreadSafeFork(1);
            var rightFork = new ThreadSafeFork(2);
            
            leftFork.TryAcquire("ТестовыйФилософ");
            rightFork.TryAcquire("ТестовыйФилософ");
            
            var processor = new MultithreadedPhilosopherStateProcessor(
                philosopher, leftFork, rightFork, _config, _strategyMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            var cts = new CancellationTokenSource();

            // Act
            await processor.ProcessEatingStateAsync(cts.Token);

            // Assert
            // После обработки состояния Eating философ должен:
            // 1) Вернуться к размышлениям
            Assert.Equal(PhilosopherState.Thinking, philosopher.State);
            // 2) Освободить вилки
            Assert.False(philosopher.HasLeftFork, "Должен освободить левую вилку");
            Assert.False(philosopher.HasRightFork, "Должен освободить правую вилку");
            Assert.Equal(ForkState.Available, leftFork.State);
            Assert.Equal(ForkState.Available, rightFork.State);
            // 3) Должно быть установлено новое время размышлений
            Assert.InRange(philosopher.StepsRemaining, _config.ThinkingTimeMin, _config.ThinkingTimeMax);
        }

        // Тест: Проверяет обработку состояния Thinking
        // Что проверяем:
        // 1. Уменьшение оставшегося времени размышлений
        // 2. Переход в Hungry по окончании времени
        // 3. Начало отсчета времени ожидания в метриках
        [Fact]
        public async Task ProcessThinkingState_TransitionsToHungry_WhenTimeExpires()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                State = PhilosopherState.Thinking,
                StepsRemaining = 1 // Очень короткое время размышлений
            };
            
            var leftFork = new ThreadSafeFork(1);
            var rightFork = new ThreadSafeFork(2);
            
            var processor = new MultithreadedPhilosopherStateProcessor(
                philosopher, leftFork, rightFork, _config, _strategyMock.Object, 
                _acquisitionManager, _metricsMock.Object);

            var cts = new CancellationTokenSource();

            // Act
            await processor.ProcessThinkingStateAsync(cts.Token);

            // Assert
            // 1) Должен проголодаться
            Assert.Equal(PhilosopherState.Hungry, philosopher.State);
            // 2) Таймер должен обнулиться
            Assert.Equal(0, philosopher.StepsRemaining);
            // 3) Действие должно быть сброшено
            Assert.Equal(PhilosopherAction.None, philosopher.CurrentAction);
            
            // Метрика должна зафиксировать начало ожидания
            _metricsMock.Verify(m => m.StartWaiting("ТестовыйФилософ"), Times.Once());
        }

        // Вспомогательный метод: ожидает выполнения условия
        // "condition" - Условие для проверки
        // "timeoutToken" - Токен отмены для таймаута
        // "checkIntervalMs" - Интервал проверки в миллисекундах
        private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken timeoutToken, int checkIntervalMs = 10)
        {
            var startTime = DateTime.UtcNow;
            
            while (!condition())
            {
                if (timeoutToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Условие не выполнено за отведенное время. Прошло: {(DateTime.UtcNow - startTime).TotalSeconds:F1} сек");
                }
                
                await Task.Delay(checkIntervalMs, timeoutToken);
            }
        }

        // Перегрузка для удобства с таймаутом в миллисекундах
        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            await WaitUntilAsync(condition, cts.Token);
        }
    }
}