using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Metrics;
using Xunit;

namespace DiningPhilosophers.Tests.Metrics
{
    /// Тесты для коллектора метрик пошаговой симуляции.
    /// Проверяет сбор и агрегацию статистики без учета времени выполнения.
    public class MetricsCollectorTests
    {
        // Тест: Проверяет инициализацию коллектора.
        // Что проверяем:
        // 1. Коллектор создает метрики для всех философов
        // 2. Коллектор создает метрики для всех вилок
        // 3. Исходные значения равны 0
        [Fact]
        public void Constructor_CreatesMetrics_ForAllPhilosophersAndForks()
        {
            // Arrange
            var philosophers = new List<Philosopher>
            {
                new Philosopher("Платон"),
                new Philosopher("Аристотель"),
                new Philosopher("Сократ")
            };
            
            var forks = new List<Fork>
            {
                new Fork(1),
                new Fork(2),
                new Fork(3)
            };

            // Act
            var collector = new MetricsCollector(philosophers, forks);

            // Assert
            // Проверяем метрики философов
            foreach (var philosopher in philosophers)
            {
                var metrics = collector.GetPhilosopherMetrics(philosopher.Name);
                Assert.NotNull(metrics);
                Assert.Equal(0, metrics.MealsEaten);
                Assert.Equal(0, metrics.WaitingSteps);
            }

            // Проверяем метрики вилок
            foreach (var fork in forks)
            {
                var metrics = collector.GetForkMetrics(fork.Id);
                Assert.NotNull(metrics);
                Assert.Equal(0, metrics.StepsFree);
                Assert.Equal(0, metrics.StepsBlocked);
                Assert.Equal(0, metrics.StepsInUse);
            }
        }

        // Тест: Проверяет увеличение счетчика съеденных трапез.
        // Что проверяем:
        // 1. IncrementMeal увеличивает MealsEaten на 1
        // 2. Метрики разных философов независимы
        [Fact]
        public void IncrementMeal_IncreasesMealsEaten_ForSpecificPhilosopher()
        {
            // Arrange
            var philosophers = new List<Philosopher>
            {
                new Philosopher("Платон"),
                new Philosopher("Аристотель")
            };
            
            var forks = new List<Fork> { new Fork(1) };
            var collector = new MetricsCollector(philosophers, forks);

            // Act
            collector.IncrementMeal("Платон");
            collector.IncrementMeal("Платон"); // Вторая трапеза
            collector.IncrementMeal("Аристотель"); // Трапеза другого философа

            // Assert
            var platoMetrics = collector.GetPhilosopherMetrics("Платон");
            var aristotleMetrics = collector.GetPhilosopherMetrics("Аристотель");
            
            Assert.Equal(2, platoMetrics.MealsEaten);
            Assert.Equal(1, aristotleMetrics.MealsEaten);
        }

        // Тест: Проверяет увеличение счетчика шагов ожидания.
        // Что проверяем:
        // 1. IncrementWaiting увеличивает WaitingSteps на 1
        // 2. Каждый вызов учитывается отдельно
        [Fact]
        public void IncrementWaiting_IncreasesWaitingSteps_ForSpecificPhilosopher()
        {
            // Arrange
            var philosophers = new List<Philosopher>
            {
                new Philosopher("Платон"),
                new Philosopher("Аристотель")
            };
            
            var forks = new List<Fork> { new Fork(1) };
            var collector = new MetricsCollector(philosophers, forks);

            // Act
            collector.IncrementWaiting("Платон");
            collector.IncrementWaiting("Платон");
            collector.IncrementWaiting("Платон"); // 3 шага ожидания
            collector.IncrementWaiting("Аристотель"); // 1 шаг ожидания

            // Assert
            var platoMetrics = collector.GetPhilosopherMetrics("Платон");
            var aristotleMetrics = collector.GetPhilosopherMetrics("Аристотель");
            
            Assert.Equal(3, platoMetrics.WaitingSteps);
            Assert.Equal(1, aristotleMetrics.WaitingSteps);
        }

        // Тест: Проверяет запись использования вилки.
        // Что проверяем:
        // 1. RecordForkUsage учитывает состояние вилки
        // 2. Разные состояния (Available, InUse) учитываются правильно
        // 3. Учитывается владелец и его состояние
        [Fact]
        public void RecordForkUsage_RecordsCorrectState_ForAvailableFork()
        {
            // Arrange
            var philosopher = new Philosopher("Платон") { State = PhilosopherState.Thinking };
            var philosophers = new List<Philosopher> { philosopher };
            
            var fork = new Fork(1) { State = ForkState.Available };
            var forks = new List<Fork> { fork };
            
            var collector = new MetricsCollector(philosophers, forks);

            // Act - вилка свободна
            collector.RecordForkUsage(fork, philosophers);

            // Assert
            var metrics = collector.GetForkMetrics(1);
            // Свободная вилка должна увеличить StepsFree
            Assert.Equal(1, metrics.StepsFree);
            Assert.Equal(0, metrics.StepsBlocked);
            Assert.Equal(0, metrics.StepsInUse);
        }

        // Тест: Проверяет запись использования вилки для еды.
        // Что проверяем:
        // 1. Вилка InUse с владельцем, который ест → StepsInUse
        // 2. Вилка InUse с владельцем, который не ест → StepsBlocked
        [Theory]
        [InlineData(PhilosopherState.Eating, 0, 0, 1)]    // Владелец ест → InUse
        [InlineData(PhilosopherState.Hungry, 0, 1, 0)]   // Владелец голоден → Blocked
        [InlineData(PhilosopherState.Thinking, 0, 1, 0)] // Владелец думает → Blocked
        public void RecordForkUsage_RecordsCorrectState_ForInUseFork(
            PhilosopherState ownerState,
            int expectedFree,
            int expectedBlocked,
            int expectedInUse)
        {
            // Arrange
            var philosopher = new Philosopher("Платон") { State = ownerState };
            var philosophers = new List<Philosopher> { philosopher };
            
            var fork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "Платон" 
            };
            
            var forks = new List<Fork> { fork };
            var collector = new MetricsCollector(philosophers, forks);

            // Act
            collector.RecordForkUsage(fork, philosophers);

            // Assert
            var metrics = collector.GetForkMetrics(1);
            Assert.Equal(expectedFree, metrics.StepsFree);
            Assert.Equal(expectedBlocked, metrics.StepsBlocked);
            Assert.Equal(expectedInUse, metrics.StepsInUse);
        }

        // Тест: Проверяет запись использования вилки без владельца.
        // Что проверяем:
        // 1. Вилка InUse без владельца → StepsBlocked
        // 2. Владелец не найден среди философов → StepsBlocked
        [Fact]
        public void RecordForkUsage_RecordsBlocked_WhenNoOwnerOrOwnerNotFound()
        {
            // Arrange
            var philosophers = new List<Philosopher>
            {
                new Philosopher("Платон")
            };
            
            // Вилка InUse без владельца
            var forkNoOwner = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = null 
            };
            
            // Вилка InUse с несуществующим владельцем
            var forkWrongOwner = new Fork(2) 
            { 
                State = ForkState.InUse, 
                Owner = "НесуществующийФилософ" 
            };
            
            var forks = new List<Fork> { forkNoOwner, forkWrongOwner };
            var collector = new MetricsCollector(philosophers, forks);

            // Act
            collector.RecordForkUsage(forkNoOwner, philosophers);
            collector.RecordForkUsage(forkWrongOwner, philosophers);

            // Assert
            var metrics1 = collector.GetForkMetrics(1);
            var metrics2 = collector.GetForkMetrics(2);
            
            Assert.Equal(0, metrics1.StepsFree);
            // Вилка без владельца должна быть Blocked
            Assert.Equal(1, metrics1.StepsBlocked);
            Assert.Equal(0, metrics1.StepsInUse);
            
            Assert.Equal(0, metrics2.StepsFree);
            // Вилка с несуществующим владельцем должна быть Blocked
            Assert.Equal(1, metrics2.StepsBlocked);
            Assert.Equal(0, metrics2.StepsInUse);
        }

        // Тест: Проверяет сброс всех метрик.
        // Что проверяем:
        // 1. Reset обнуляет метрики философов
        // 2. Reset обнуляет метрики вилок
        // 3. После Reset можно снова накапливать метрики
        [Fact]
        public void Reset_ClearsAllMetrics_Completely()
        {
            // Arrange
            var philosopher = new Philosopher("Платон") { State = PhilosopherState.Eating };
            var philosophers = new List<Philosopher> { philosopher };
            
            var fork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "Платон" 
            };
            
            var forks = new List<Fork> { fork };
            var collector = new MetricsCollector(philosophers, forks);
            
            // Накопление метрик
            collector.IncrementMeal("Платон");
            collector.IncrementWaiting("Платон");
            collector.RecordForkUsage(fork, philosophers);

            // Act
            collector.Reset();

            // Assert
            var philosopherMetrics = collector.GetPhilosopherMetrics("Платон");
            var forkMetrics = collector.GetForkMetrics(1);
            
            // Метрики философа обнулены
            Assert.Equal(0, philosopherMetrics.MealsEaten);
            Assert.Equal(0, philosopherMetrics.WaitingSteps);
            
            // Метрики вилки обнулены
            Assert.Equal(0, forkMetrics.StepsFree);
            Assert.Equal(0, forkMetrics.StepsBlocked);
            Assert.Equal(0, forkMetrics.StepsInUse);
            
            // После Reset можно снова накапливать метрики
            collector.IncrementMeal("Платон");
            var newMetrics = collector.GetPhilosopherMetrics("Платон");
            Assert.Equal(1, newMetrics.MealsEaten);
        }

        // Тест: Проверяет получение метрик для несуществующего философа.
        // Что проверяем:
        // 1. GetPhilosopherMetrics бросает исключение для несуществующего имени
        // 2. Исключение содержит информацию об ошибке
        [Fact]
        public void GetPhilosopherMetrics_ThrowsException_ForNonExistentPhilosopher()
        {
            // Arrange
            var philosophers = new List<Philosopher>
            {
                new Philosopher("Платон")
            };
            
            var forks = new List<Fork> { new Fork(1) };
            var collector = new MetricsCollector(philosophers, forks);

            // Act & Assert
            var exception = Assert.Throws<KeyNotFoundException>(() => 
                collector.GetPhilosopherMetrics("НесуществующийФилософ"));
            
            Assert.Contains("НесуществующийФилософ", exception.Message);
        }

        // Тест: Проверяет получение метрик для несуществующей вилки.
        // Что проверяем:
        // 1. GetForkMetrics бросает исключение для несуществующего ID
        [Fact]
        public void GetForkMetrics_ThrowsException_ForNonExistentFork()
        {
            // Arrange
            var philosophers = new List<Philosopher> { new Philosopher("Платон") };
            var forks = new List<Fork> { new Fork(1) };
            var collector = new MetricsCollector(philosophers, forks);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => 
                collector.GetForkMetrics(999));
        }

        // Тест: Проверяет расчет TotalObservedSteps для вилки.
        // Что проверяем:
        // 1. TotalObservedSteps = StepsFree + StepsBlocked + StepsInUse
        // 2. Свойство корректно вычисляет сумму
        [Theory]
        [InlineData(5, 3, 2, 10)] // 5+3+2=10
        [InlineData(0, 0, 0, 0)]  // Все нули
        [InlineData(100, 0, 0, 100)] // Только свободна
        [InlineData(0, 50, 50, 100)] // Заблокирована и используется
        public void ForkMetrics_TotalObservedSteps_CalculatesCorrectly(
            long stepsFree, long stepsBlocked, long stepsInUse, long expectedTotal)
        {
            // Arrange
            var metrics = new ForkMetrics
            {
                StepsFree = stepsFree,
                StepsBlocked = stepsBlocked,
                StepsInUse = stepsInUse
            };

            // Act
            var total = metrics.TotalObservedSteps;

            // Assert
            Assert.Equal(expectedTotal, total);
        }

        // Тест: Проверяет сброс метрик вилки.
        // Что проверяем:
        // 1. Reset обнуляет все счетчики вилки
        // 2. После Reset TotalObservedSteps = 0
        [Fact]
        public void ForkMetrics_Reset_SetsAllCountersToZero()
        {
            // Arrange
            var metrics = new ForkMetrics
            {
                StepsFree = 10,
                StepsBlocked = 5,
                StepsInUse = 3
            };

            // Act
            metrics.Reset();

            // Assert
            Assert.Equal(0, metrics.StepsFree);
            Assert.Equal(0, metrics.StepsBlocked);
            Assert.Equal(0, metrics.StepsInUse);
            Assert.Equal(0, metrics.TotalObservedSteps);
        }

        // Тест: Проверяет сброс метрик философа.
        // Что проверяем:
        // 1. Reset обнуляет MealsEaten и WaitingSteps
        [Fact]
        public void PhilosopherMetrics_Reset_SetsAllCountersToZero()
        {
            // Arrange
            var metrics = new PhilosopherMetrics
            {
                MealsEaten = 7,
                WaitingSteps = 12
            };

            // Act
            metrics.Reset();

            // Assert
            Assert.Equal(0, metrics.MealsEaten);
            Assert.Equal(0, metrics.WaitingSteps);
        }

        // Тест: Проверяет агрегацию метрик для нескольких вилок.
        // Что проверяем:
        // 1. RecordForkUsage корректно работает для разных вилок
        // 2. Метрики разных вилок независимы
        [Fact]
        public void RecordForkUsage_AggregatesMetrics_ForMultipleForks()
        {
            // Arrange
            var philosopher = new Philosopher("Платон") { State = PhilosopherState.Eating };
            var philosophers = new List<Philosopher> { philosopher };
            
            var fork1 = new Fork(1) { State = ForkState.Available };
            var fork2 = new Fork(2) 
            { 
                State = ForkState.InUse, 
                Owner = "Платон" 
            };
            
            var fork3 = new Fork(3) 
            { 
                State = ForkState.InUse, 
                Owner = "Платон" 
            };
            
            var forks = new List<Fork> { fork1, fork2, fork3 };
            var collector = new MetricsCollector(philosophers, forks);

            // Act - многократная запись
            for (int i = 0; i < 3; i++)
            {
                collector.RecordForkUsage(fork1, philosophers); // 3x Free
                collector.RecordForkUsage(fork2, philosophers); // 3x InUse (владелец ест)
            }
            
            // fork3 не записываем

            // Assert
            var metrics1 = collector.GetForkMetrics(1);
            var metrics2 = collector.GetForkMetrics(2);
            var metrics3 = collector.GetForkMetrics(3);
            
            // Вилка 1: 3 раза свободна
            Assert.Equal(3, metrics1.StepsFree);
            Assert.Equal(0, metrics1.StepsBlocked);
            Assert.Equal(0, metrics1.StepsInUse);
            
            // Вилка 2: 3 раза используется для еды
            Assert.Equal(0, metrics2.StepsFree);
            Assert.Equal(0, metrics2.StepsBlocked);
            Assert.Equal(3, metrics2.StepsInUse);
            
            // Вилка 3: не записывалась, все 0
            Assert.Equal(0, metrics3.StepsFree);
            Assert.Equal(0, metrics3.StepsBlocked);
            Assert.Equal(0, metrics3.StepsInUse);
        }
    }
}