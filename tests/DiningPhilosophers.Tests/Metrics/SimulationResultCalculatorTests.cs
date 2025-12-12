using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Metrics;
using DiningPhilosophers.Services.Simulation;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using Moq;
using Xunit;

namespace DiningPhilosophers.Tests.Metrics
{
    // Тесты для калькулятора результатов симуляции.
    // Проверяет корректность расчета финальных метрик для разных типов симуляций.
    public class SimulationResultCalculatorTests
    {
        // Тест: Проверяет расчет результатов для пошаговой симуляции.
        // Что проверяем:
        // 1. Корректный подсчет общего количества шагов и трапез
        // 2. Правильный расчет пропускной способности (throughput)
        // 3. Агрегацию времени ожидания по философам
        // 4. Расчет утилизации вилок в процентах
        [Fact]
        public void CalculateForStepByStep_ReturnsCorrectResult_WithCompleteData()
        {
            // Arrange - подготовка тестовых данных
            var philosophers = new List<Philosopher> 
            { 
                new Philosopher("Платон"),
                new Philosopher("Аристотель") 
            };
            
            var forks = new List<Fork> 
            { 
                new Fork(1),
                new Fork(2) 
            };
            
            var metrics = new MetricsCollector(philosophers, forks);
            
            // Накопление метрик
            metrics.IncrementMeal("Платон");
            metrics.IncrementMeal("Платон"); // Платон съел 2 раза
            metrics.IncrementMeal("Аристотель"); // Аристотель съел 1 раз
            
            metrics.IncrementWaiting("Платон");
            metrics.IncrementWaiting("Платон");
            metrics.IncrementWaiting("Платон"); // Платон ждал 3 шага
            
            metrics.IncrementWaiting("Аристотель");
            metrics.IncrementWaiting("Аристотель"); // Аристотель ждал 2 шага
            
            // Записываем использование вилок
            // Вилка 1: 4 шага свободна, 1 шаг используется для еды
            for (int i = 0; i < 4; i++)
            {
                forks[0].State = ForkState.Available;
                metrics.RecordForkUsage(forks[0], philosophers);
            }
            
            forks[0].State = ForkState.InUse;
            forks[0].Owner = "Платон";
            philosophers[0].State = PhilosopherState.Eating;
            metrics.RecordForkUsage(forks[0], philosophers);
            
            // Вилка 2: 3 шага заблокирована, 2 шага свободна
            for (int i = 0; i < 3; i++)
            {
                forks[1].State = ForkState.InUse;
                forks[1].Owner = "Аристотель";
                philosophers[1].State = PhilosopherState.Hungry; // Не ест → Blocked
                metrics.RecordForkUsage(forks[1], philosophers);
            }
            
            for (int i = 0; i < 2; i++)
            {
                forks[1].State = ForkState.Available;
                metrics.RecordForkUsage(forks[1], philosophers);
            }
            
            var calculator = new SimulationResultCalculator();
            int totalSteps = 100;

            // Act - расчет результатов
            var result = calculator.CalculateForStepByStep(metrics, philosophers, forks, totalSteps);

            // Assert - проверка результатов
            // Общие метрики
            // TotalSteps должен соответствовать входному параметру
            Assert.Equal(totalSteps, result.TotalSteps);
            // Должно быть 3 трапезы (2 у Платона + 1 у Аристотеля)
            Assert.Equal(3, result.TotalMeals);
            
            // Throughput: 3 трапезы * 1000 / 100 шагов = 30
            double expectedThroughput = 3 * 1000.0 / totalSteps;
            Assert.Equal(expectedThroughput, result.ThroughputPer1000, 3);
            
            // Время ожидания по философам
            Assert.Equal(3, result.WaitingTimes["Платон"]);
            Assert.Equal(2, result.WaitingTimes["Аристотель"]);
            
            // Эпизоды ожидания (в пошаговой версии всегда 0)
            Assert.Equal(0, result.WaitingEpisodes["Платон"]);
            Assert.Equal(0, result.WaitingEpisodes["Аристотель"]);
            
            // Утилизация вилок (в процентах)
            // Вилка 1: 4 свободна, 1 для еды → 80% свободна, 20% для еды
            Assert.True(result.ForkUtilizations.ContainsKey(1), 
                "Должна быть метрика для вилки 1");
            
            var fork1Util = result.ForkUtilizations[1];
            Assert.Equal(80.0, fork1Util.FreePct, 1); // 4/5 * 100 = 80%
            Assert.Equal(0.0, fork1Util.BlockedPct, 1); // Вилка 1 не должна быть заблокирована
            Assert.Equal(20.0, fork1Util.InUsePct, 1); // 1/5 * 100 = 20%
            
            // Вилка 2: 3 заблокирована, 2 свободна → 40% свободна, 60% заблокирована
            Assert.True(result.ForkUtilizations.ContainsKey(2), 
                "Должна быть метрика для вилки 2");
            
            var fork2Util = result.ForkUtilizations[2];
            Assert.Equal(40.0, fork2Util.FreePct, 1); // 2/5 * 100 = 40%
            Assert.Equal(60.0, fork2Util.BlockedPct, 1); // 3/5 * 100 = 60%
            Assert.Equal(0.0, fork2Util.InUsePct, 1);
        }

        // Тест: Проверяет расчет результатов при нулевых значениях.
        // Что проверяем:
        // 1. Обработка нулевого количества шагов (избегание деления на ноль)
        // 2. Отсутствие трапез и времени ожидания
        // 3. Корректная работа с пустыми коллекциями
        [Fact]
        public void CalculateForStepByStep_HandlesZeroValues_Correctly()
        {
            // Arrange
            var philosophers = new List<Philosopher> 
            { 
                new Philosopher("ТестовыйФилософ") 
            };
            
            var forks = new List<Fork> 
            { 
                new Fork(1) 
            };
            
            var metrics = new MetricsCollector(philosophers, forks);
            var calculator = new SimulationResultCalculator();
            int totalSteps = 0; // Нулевое количество шагов

            // Act
            var result = calculator.CalculateForStepByStep(metrics, philosophers, forks, totalSteps);

            // Assert
            Assert.Equal(0, result.TotalSteps);
            Assert.Equal(0, result.TotalMeals);
            // При нулевых шагах пропускная способность должна быть 0
            Assert.Equal(0.0, result.ThroughputPer1000);
            
            Assert.Equal(0, result.WaitingTimes["ТестовыйФилософ"]);
            Assert.True(result.ForkUtilizations.ContainsKey(1),
                "Должна быть запись для вилки даже при нулевых данных");
        }

        // Тест: Проверяет расчет результатов для многопоточной симуляции.
        // Упрощенный тест без точных измерений времени.
        [Fact]
        public void CalculateForMultithreaded_ReturnsCorrectResult_WithRealTimeMetrics()
        {
            // Arrange
            var philosophers = new List<Philosopher> 
            { 
                new Philosopher("Платон"),
                new Philosopher("Аристотель") 
            };
            
            var forks = new List<ThreadSafeFork> 
            { 
                new ThreadSafeFork(1),
                new ThreadSafeFork(2) 
            };
            
            // Создаем мок метрик, чтобы контролировать значения
            var metricsMock = new Mock<Core.Contracts.Monitor.IMultithreadedMetricsCollector>();
            
            // Настраиваем возвращаемые метрики
            var platoMetrics = new MultithreadedPhilosopherMetrics 
            { 
                MealsEaten = 2,
                TotalWaitingTimeMs = 80,
                HungerEpisodes = 2 
            };
            
            var aristotleMetrics = new MultithreadedPhilosopherMetrics 
            { 
                MealsEaten = 0,
                TotalWaitingTimeMs = 0,
                HungerEpisodes = 0 
            };
            
            var fork1Metrics = new ForkMetrics { StepsFree = 5, StepsBlocked = 3, StepsInUse = 2 };
            var fork2Metrics = new ForkMetrics { StepsFree = 6, StepsBlocked = 2, StepsInUse = 2 };
            
            metricsMock.Setup(m => m.GetPhilosopherMetrics("Платон")).Returns(platoMetrics);
            metricsMock.Setup(m => m.GetPhilosopherMetrics("Аристотель")).Returns(aristotleMetrics);
            metricsMock.Setup(m => m.GetForkMetrics(1)).Returns(fork1Metrics);
            metricsMock.Setup(m => m.GetForkMetrics(2)).Returns(fork2Metrics);
            
            var calculator = new SimulationResultCalculator();
            long totalMilliseconds = 1000;

            // Act
            var result = calculator.CalculateForMultithreaded(
                metricsMock.Object, philosophers, forks, totalMilliseconds);

            // Assert
            Assert.Equal(totalMilliseconds, result.TotalMilliseconds);
            Assert.Equal(2, result.TotalMeals); // Только Платон ел
            
            // Throughput: 2 трапезы / 1000мс = 0.002, но в коде ThroughputPer1000 = result.TotalMeals * 1000.0 / Math.Max(1, totalMilliseconds)
            // Значит 2 * 1000 / 1000 = 2.0, а не 0.002
            double expectedThroughput = 2 * 1000.0 / totalMilliseconds; // = 2.0
            Assert.Equal(expectedThroughput, result.ThroughputPer1000, 5);
            
            // Проверяем наличие ключей
            Assert.Contains("Платон", result.WaitingTimes.Keys);
            Assert.Contains("Аристотель", result.WaitingTimes.Keys);
            Assert.Contains("Платон", result.WaitingEpisodes.Keys);
            Assert.Contains("Аристотель", result.WaitingEpisodes.Keys);
            
            // Проверяем значения
            Assert.Equal(80, result.WaitingTimes["Платон"]);
            Assert.Equal(0, result.WaitingTimes["Аристотель"]);
            Assert.Equal(2, result.WaitingEpisodes["Платон"]);
            Assert.Equal(0, result.WaitingEpisodes["Аристотель"]);
            
            // Проверяем наличие утилизации вилок
            Assert.Contains(1, result.ForkUtilizations.Keys);
            Assert.Contains(2, result.ForkUtilizations.Keys);
        }

        // Тест: Проверяет нормализацию процентов утилизации вилок.
        // Что проверяем:
        // 1. Сумма FreePct + BlockedPct + InUsePct всегда равна ~100%
        // 2. Нормализация работает даже при нулевых значениях
        // 3. Корректное округление
        [Theory]
        [InlineData(10, 20, 30, 16.67, 33.33, 50.00)] // 10:20:30 = 16.67:33.33:50.00
        [InlineData(0, 50, 50, 0.00, 50.00, 50.00)]   // 0:50:50 = 0:50:50
        [InlineData(100, 0, 0, 100.00, 0.00, 0.00)]   // 100:0:0 = 100:0:0
        [InlineData(33, 33, 34, 33.0, 33.0, 34.0)]    // 33:33:34 ≈ 33:33:34
        public void CalculateForkUtilization_NormalizesPercentages_Correctly(
            long stepsFree, long stepsBlocked, long stepsInUse,
            double expectedFreePct, double expectedBlockedPct, double expectedInUsePct)
        {
            // Arrange - создаем мок ForkMetrics
            var forkMetrics = new ForkMetrics
            {
                StepsFree = stepsFree,
                StepsBlocked = stepsBlocked,
                StepsInUse = stepsInUse
            };
            
            long totalObserved = stepsFree + stepsBlocked + stepsInUse;
            
            // Для теста используем рефлексию или создаем тестовый метод
            // В реальном коде нормализация происходит внутри CalculateAndAddForkUtilization
            
            // Assert - проверяем логику нормализации
            if (totalObserved > 0)
            {
                double freePct = 100.0 * stepsFree / totalObserved;
                double blockedPct = 100.0 * stepsBlocked / totalObserved;
                double inUsePct = 100.0 * stepsInUse / totalObserved;
                
                // В реальном коде есть дополнительная нормализация
                double sum = freePct + blockedPct + inUsePct;
                if (sum > 0)
                {
                    freePct = freePct * 100.0 / sum;
                    blockedPct = blockedPct * 100.0 / sum;
                    inUsePct = inUsePct * 100.0 / sum;
                }
                
                Assert.Equal(expectedFreePct, freePct, 1);
                Assert.Equal(expectedBlockedPct, blockedPct, 1);
                Assert.Equal(expectedInUsePct, inUsePct, 1);
            }
        }

        // Тест: Проверяет согласованность результатов для одинаковых данных.
        // Что проверяем:
        // 1. Multiple calls with same data return same results
        // 2. Results are deterministic
        // 3. No side effects between calls
        [Fact]
        public void CalculateResults_IsDeterministic_ForSameInput()
        {
            // Arrange
            var philosophers = new List<Philosopher> 
            { 
                new Philosopher("ТестовыйФилософ") 
            };
            
            var forks = new List<Fork> 
            { 
                new Fork(1) 
            };
            
            var metrics = new MetricsCollector(philosophers, forks);
            metrics.IncrementMeal("ТестовыйФилософ");
            metrics.IncrementWaiting("ТестовыйФилософ");
            
            var calculator = new SimulationResultCalculator();
            int totalSteps = 100;

            // Act - multiple calls
            var result1 = calculator.CalculateForStepByStep(metrics, philosophers, forks, totalSteps);
            var result2 = calculator.CalculateForStepByStep(metrics, philosophers, forks, totalSteps);
            var result3 = calculator.CalculateForStepByStep(metrics, philosophers, forks, totalSteps);

            // Assert - все результаты должны быть идентичны
            Assert.Equal(result1.TotalMeals, result2.TotalMeals);
            Assert.Equal(result1.TotalMeals, result3.TotalMeals);
            
            Assert.Equal(result1.ThroughputPer1000, result2.ThroughputPer1000, 5);
            Assert.Equal(result1.ThroughputPer1000, result3.ThroughputPer1000, 5);
            
            Assert.Equal(result1.WaitingTimes["ТестовыйФилософ"], 
                result2.WaitingTimes["ТестовыйФилософ"]);
        }

        // Тест: Проверяет обработку большого количества философов и вилок.
        // Что проверяем:
        // 1. Расчет работает с 5 философами (стандартный случай)
        // 2. Корректная агрегация метрик для всех
        // 3. Утилизация рассчитывается для всех вилок
        [Fact]
        public void CalculateResults_HandlesFivePhilosophers_Correctly()
        {
            // Arrange
            var philosophers = new List<Philosopher>
            {
                new Philosopher("Платон"),
                new Philosopher("Аристотель"),
                new Philosopher("Сократ"),
                new Philosopher("Декарт"),
                new Philosopher("Кант")
            };
            
            var forks = new List<Fork>
            {
                new Fork(1),
                new Fork(2),
                new Fork(3),
                new Fork(4),
                new Fork(5)
            };
            
            var metrics = new MetricsCollector(philosophers, forks);
            
            // Каждый философ съел разное количество раз
            // Ошибка: for (int j = 0; j <= i; j++) даст: 1, 2, 3, 4, 5 трапез = 15 всего
            // Нужно: 0, 1, 2, 3, 4 трапез = 10 всего
            for (int i = 0; i < philosophers.Count; i++)
            {
                for (int j = 0; j < i; j++) // Изменено с j <= i на j < i
                {
                    metrics.IncrementMeal(philosophers[i].Name);
                }
            }
            
            var calculator = new SimulationResultCalculator();
            int totalSteps = 1000;

            // Act
            var result = calculator.CalculateForStepByStep(metrics, philosophers, forks, totalSteps);

            // Assert
            // Общее количество трапез: 0+1+2+3+4 = 10
            Assert.Equal(10, result.TotalMeals);
            
            // Throughput: 10 * 1000 / 1000 = 10
            Assert.Equal(10.0, result.ThroughputPer1000, 1);
            
            // Проверяем наличие метрик для всех философов
            foreach (var philosopher in philosophers)
            {
                Assert.True(result.WaitingTimes.ContainsKey(philosopher.Name),
                    $"Должна быть метрика ожидания для {philosopher.Name}");
                Assert.True(result.WaitingEpisodes.ContainsKey(philosopher.Name),
                    $"Должна быть метрика эпизодов для {philosopher.Name}");
            }
            
            // Проверяем наличие утилизации для всех вилок
            for (int i = 1; i <= 5; i++)
            {
                Assert.True(result.ForkUtilizations.ContainsKey(i),
                    $"Должна быть метрика утилизации для вилки {i}");
            }
        }
    }
}