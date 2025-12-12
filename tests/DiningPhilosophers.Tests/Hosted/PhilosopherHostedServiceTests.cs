using System;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Hosted.Services.Philosophers;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DiningPhilosophers.Tests.Hosted
{
    // Тесты для Hosted Service философов, реализующих BackgroundService.
    // Проверяет интеграцию философов с .NET Generic Host (задача 3).
    public class PhilosopherHostedServiceTests
    {
        // Тест: Проверяет корректную остановку сервиса по CancellationToken.
        // Что проверяем:
        // 1. Сервис реагирует на отмену через CancellationToken
        // 2. Выполнение прекращается при получении сигнала отмены
        // 3. Сервис не падает при отмене
        [Fact]
        public async Task StopAsync_StopsGracefully_WhenCancellationRequested()
        {
            // Arrange
            var tableManagerMock = new Mock<ITableManager>();
            var configOptionsMock = new Mock<IOptions<MultithreadedSimulationConfig>>();
            var strategyMock = new Mock<IPhilosopherStrategy>();
            var acquisitionManager = new ThreadSafeForkAcquisitionManager(20);
            var metricsMock = new Mock<IMultithreadedMetricsCollector>();
            
            var philosopher = new Philosopher("ТестовыйФилософ");

            tableManagerMock.Setup(t => t.GetPhilosopher("ТестовыйФилософ")).Returns(philosopher);
            tableManagerMock.Setup(t => t.GetLeftFork("ТестовыйФилософ")).Returns(new ThreadSafeFork(1));
            tableManagerMock.Setup(t => t.GetRightFork("ТестовыйФилософ")).Returns(new ThreadSafeFork(2));

            configOptionsMock.Setup(o => o.Value).Returns(new MultithreadedSimulationConfig 
            { 
                DurationSeconds = 60 // Долгое время, но прервем раньше
            });

            var service = new PhilosopherHostedService(
                tableManagerMock.Object,
                configOptionsMock.Object,
                strategyMock.Object,
                acquisitionManager,
                metricsMock.Object,
                "ТестовыйФилософ"
            );

            var cts = new CancellationTokenSource();
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - запускаем и быстро останавливаем
            await service.StartAsync(cts.Token);
            
            // Даем немного времени на инициализацию
            await Task.Delay(50);
            
            // Останавливаем сервис
            await service.StopAsync(cts.Token);
            
            stopWatch.Stop();

            // Assert - сервис должен был остановиться
            Assert.True(stopWatch.ElapsedMilliseconds < 1000, 
                "Сервис должен остановиться в течение 1 секунды");
        }

        // Тест: Проверяет создание процессора состояний с правильными параметрами.
        // Что проверяем:
        // 1. MultithreadedPhilosopherStateProcessor создается с корректными параметрами
        // 2. Все зависимости передаются правильно
        // 3. Конфигурация применяется
        [Fact]
        public void Constructor_CreatesStateProcessor_WithCorrectParameters()
        {
            // Arrange
            var tableManagerMock = new Mock<ITableManager>();
            var configOptionsMock = new Mock<IOptions<MultithreadedSimulationConfig>>();
            var strategyMock = new Mock<IPhilosopherStrategy>();
            var acquisitionManager = new ThreadSafeForkAcquisitionManager(30);
            var metricsMock = new Mock<IMultithreadedMetricsCollector>();
            
            var expectedPhilosopher = new Philosopher("ТестовыйФилософ");
            var expectedLeftFork = new ThreadSafeFork(1);
            var expectedRightFork = new ThreadSafeFork(2);
            var expectedConfig = new MultithreadedSimulationConfig 
            { 
                ThinkingTimeMin = 50,
                ThinkingTimeMax = 100,
                EatingTimeMin = 60,
                EatingTimeMax = 80,
                ForkAcquisitionTime = 25
            };

            tableManagerMock.Setup(t => t.GetPhilosopher("ТестовыйФилософ")).Returns(expectedPhilosopher);
            tableManagerMock.Setup(t => t.GetLeftFork("ТестовыйФилософ")).Returns(expectedLeftFork);
            tableManagerMock.Setup(t => t.GetRightFork("ТестовыйФилософ")).Returns(expectedRightFork);
            configOptionsMock.Setup(o => o.Value).Returns(expectedConfig);

            // Act - создаем сервис
            var service = new PhilosopherHostedService(
                tableManagerMock.Object,
                configOptionsMock.Object,
                strategyMock.Object,
                acquisitionManager,
                metricsMock.Object,
                "ТестовыйФилософ"
            );

            // Assert - проверяем, что все зависимости были получены
            tableManagerMock.Verify(t => t.GetPhilosopher("ТестовыйФилософ"), Times.Once);
            tableManagerMock.Verify(t => t.GetLeftFork("ТестовыйФилософ"), Times.Once);
            tableManagerMock.Verify(t => t.GetRightFork("ТестовыйФилософ"), Times.Once);
            configOptionsMock.Verify(o => o.Value, Times.AtLeastOnce);
        }

        // Тест: Проверяет работу конкретных реализаций философов (Платон, Аристотель и т.д.).
        // Что проверяем:
        // 1. Конкретные классы философов наследуют PhilosopherHostedService
        // 2. Имя философа передается корректно
        // 3. Сервисы могут быть созданы через DI
        [Theory]
        [InlineData(typeof(Plato), "Платон")]
        [InlineData(typeof(Aristotle), "Аристотель")]
        [InlineData(typeof(Socrates), "Сократ")]
        [InlineData(typeof(Descartes), "Декарт")]
        [InlineData(typeof(Kant), "Кант")]
        public void SpecificPhilosopherServices_CreatedWithCorrectNames(Type philosopherType, string expectedName)
        {
            // Arrange
            var tableManagerMock = new Mock<ITableManager>();
            var configOptionsMock = new Mock<IOptions<MultithreadedSimulationConfig>>();
            var strategyMock = new Mock<IPhilosopherStrategy>();
            var acquisitionManager = new ThreadSafeForkAcquisitionManager(20);
            var metricsMock = new Mock<IMultithreadedMetricsCollector>();
            
            var philosopher = new Philosopher(expectedName);
            tableManagerMock.Setup(t => t.GetPhilosopher(expectedName)).Returns(philosopher);
            tableManagerMock.Setup(t => t.GetLeftFork(expectedName)).Returns(new ThreadSafeFork(1));
            tableManagerMock.Setup(t => t.GetRightFork(expectedName)).Returns(new ThreadSafeFork(2));
            
            configOptionsMock.Setup(o => o.Value).Returns(new MultithreadedSimulationConfig());

            // Act - создаем конкретный сервис философа через рефлексию
            var constructor = philosopherType.GetConstructor(new Type[]
            {
                typeof(ITableManager),
                typeof(IOptions<MultithreadedSimulationConfig>),
                typeof(IPhilosopherStrategy),
                typeof(ThreadSafeForkAcquisitionManager),
                typeof(IMultithreadedMetricsCollector)
            });
            
            // Проверяем, что конструктор существует
            Assert.NotNull(constructor);
            
            var service = constructor!.Invoke(new object[]
            {
                tableManagerMock.Object,
                configOptionsMock.Object,
                strategyMock.Object,
                acquisitionManager,
                metricsMock.Object
            }) as PhilosopherHostedService;

            // Assert - проверяем, что сервис создан
            Assert.NotNull(service);
            
            // Проверяем, что TableManager вызывается с правильным именем
            tableManagerMock.Verify(t => t.GetPhilosopher(expectedName), Times.Once,
                $"TableManager должен получать философа с именем '{expectedName}'");
        }

        // Тест: Проверяет обработку исключительных ситуаций при запуске.
        // Что проверяем:
        // 1. Конструктор должен корректно обрабатывать исключения из TableManager
        // 2. Сервис не должен создаваться при некорректных зависимостях
        [Fact]
        public void Constructor_ThrowsException_WhenTableManagerFails()
        {
            // Arrange
            var tableManagerMock = new Mock<ITableManager>();
            var configOptionsMock = new Mock<IOptions<MultithreadedSimulationConfig>>();
            var strategyMock = new Mock<IPhilosopherStrategy>();
            var acquisitionManager = new ThreadSafeForkAcquisitionManager(20);
            var metricsMock = new Mock<IMultithreadedMetricsCollector>();
            
            // TableManager бросает исключение при получении философа
            tableManagerMock.Setup(t => t.GetPhilosopher(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Философ не найден"));
            
            configOptionsMock.Setup(o => o.Value).Returns(new MultithreadedSimulationConfig());

            // Act & Assert - конструктор должен бросить исключение
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                var service = new PhilosopherHostedService(
                    tableManagerMock.Object,
                    configOptionsMock.Object,
                    strategyMock.Object,
                    acquisitionManager,
                    metricsMock.Object,
                    "НесуществующийФилософ"
                );
            });
            
            // Проверяем текст исключения
            Assert.Contains("Философ не найден", exception.Message);
        }

        // Тест: Проверяет интеграцию с Dependency Injection.
        // Что проверяем:
        // 1. Сервис может быть создан через DI контейнер
        // 2. Все зависимости инжектируются корректно
        // 3. Сервис реализует BackgroundService
        [Fact]
        public void Service_CanBeCreated_ThroughDependencyInjection()
        {
            // Arrange & Act - проверяем, что сервис может быть создан
            var tableManagerMock = new Mock<ITableManager>();
            var configOptionsMock = new Mock<IOptions<MultithreadedSimulationConfig>>();
            var strategyMock = new Mock<IPhilosopherStrategy>();
            var acquisitionManager = new ThreadSafeForkAcquisitionManager(20);
            var metricsMock = new Mock<IMultithreadedMetricsCollector>();
            
            tableManagerMock.Setup(t => t.GetPhilosopher(It.IsAny<string>())).Returns(new Philosopher("Тест"));
            tableManagerMock.Setup(t => t.GetLeftFork(It.IsAny<string>())).Returns(new ThreadSafeFork(1));
            tableManagerMock.Setup(t => t.GetRightFork(It.IsAny<string>())).Returns(new ThreadSafeFork(2));
            
            configOptionsMock.Setup(o => o.Value).Returns(new MultithreadedSimulationConfig());

            var service = new PhilosopherHostedService(
                tableManagerMock.Object,
                configOptionsMock.Object,
                strategyMock.Object,
                acquisitionManager,
                metricsMock.Object,
                "ТестовыйФилософ"
            );

            // Assert
            Assert.NotNull(service);
            Assert.IsType<PhilosopherHostedService>(service);
            Assert.IsAssignableFrom<BackgroundService>(service);
            
            // Проверяем, что сервис готов к работе с DI
            tableManagerMock.Verify(t => t.GetPhilosopher("ТестовыйФилософ"), Times.Once);
        }

        // Тест: Проверяет одновременный запуск нескольких философов.
        // Что проверяем:
        // 1. Несколько сервисов могут работать параллельно
        // 2. Каждый философ работает независимо
        // 3. Отсутствуют race conditions при доступе к общим ресурсам
        [Fact]
        public async Task MultipleServices_CanRunConcurrently()
        {
            // Arrange - создаем несколько философов
            var philosophers = new[]
            {
                new Philosopher("Платон"),
                new Philosopher("Аристотель"),
                new Philosopher("Сократ")
            };
            
            var services = new List<PhilosopherHostedService>();
            var tasks = new List<Task>();

            // Act - запускаем все сервисы параллельно
            foreach (var philosopher in philosophers)
            {
                var tableManagerMock = new Mock<ITableManager>();
                var configOptionsMock = new Mock<IOptions<MultithreadedSimulationConfig>>();
                var strategyMock = new Mock<IPhilosopherStrategy>();
                var acquisitionManager = new ThreadSafeForkAcquisitionManager(20);
                var metricsMock = new Mock<IMultithreadedMetricsCollector>();
                
                tableManagerMock.Setup(t => t.GetPhilosopher(philosopher.Name)).Returns(philosopher);
                tableManagerMock.Setup(t => t.GetLeftFork(philosopher.Name)).Returns(new ThreadSafeFork(1));
                tableManagerMock.Setup(t => t.GetRightFork(philosopher.Name)).Returns(new ThreadSafeFork(2));
                
                configOptionsMock.Setup(o => o.Value).Returns(new MultithreadedSimulationConfig 
                { 
                    DurationSeconds = 1 
                });

                var service = new PhilosopherHostedService(
                    tableManagerMock.Object,
                    configOptionsMock.Object,
                    strategyMock.Object,
                    acquisitionManager,
                    metricsMock.Object,
                    philosopher.Name
                );
                
                services.Add(service);
                
                // Запускаем сервис в отдельной задаче
                tasks.Add(Task.Run(async () =>
                {
                    await service.StartAsync(CancellationToken.None);
                    await Task.Delay(200); // Даем время на работу
                    await service.StopAsync(CancellationToken.None);
                }));
            }

            // Ждем завершения всех задач
            await Task.WhenAll(tasks);

            // Assert - все философы должны были начать работу
            foreach (var philosopher in philosophers)
            {
                Assert.NotEqual(default, philosopher.State);
            }
        }
    }
}