using System;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Configuration;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Hosted.Services;
using DiningPhilosophers.Hosted.Services.Philosophers;
using DiningPhilosophers.Services.Configuration;
using DiningPhilosophers.Services.Metrics;
using DiningPhilosophers.Services.Monitor;
using DiningPhilosophers.Services.Simulation;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using DiningPhilosophers.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiningPhilosophers.Hosted
{
    internal class Program
    {
        private static DateTime _simulationStartTime;

        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Получаем lifetime для управления остановкой
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var config = host.Services.GetRequiredService<IOptions<MultithreadedSimulationConfig>>().Value;

            // Запускаем хост
            var ctSource = new CancellationTokenSource();
            lifetime.ApplicationStopping.Register(() => ctSource.Cancel());
            var startTask = host.StartAsync(ctSource.Token);

            // Ждем завершения запуска
            await startTask;

            _simulationStartTime = DateTime.Now;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(config.DurationSeconds), ctSource.Token);
            }
            catch (TaskCanceledException)
            {
            }

            // Останавливаем хост
            await host.StopAsync(ctSource.Token);

            // Выводим финальный summary после остановки
            var monitor = host.Services.GetRequiredService<IMonitor>();
            var metricsAdapter = host.Services.GetRequiredService<IMetricsCollector>();
            var multiMetrics = host.Services.GetRequiredService<IMultithreadedMetricsCollector>();
            var tableManager = host.Services.GetRequiredService<ITableManager>();
            var resultCalculator = host.Services.GetRequiredService<SimulationResultCalculator>();
            
            var totalMilliseconds = (long)(DateTime.Now - _simulationStartTime).TotalMilliseconds;
            var result = resultCalculator.CalculateForMultithreaded(
                multiMetrics, 
                tableManager.GetPhilosophers(), 
                tableManager.GetForks(), 
                totalMilliseconds);
                
            monitor.DisplaySummary(metricsAdapter, result);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Конфигурация
                    services.Configure<MultithreadedSimulationConfig>(hostContext.Configuration.GetSection("Simulation"));
                    
                    services.AddSingleton<IPhilosopherNamesProvider>(
                        new FilePhilosopherNamesProvider("philosophers.txt"));

                    services.AddSingleton<SimulationResultCalculator>();

                    // Стратегия (без координатора)
                    services.AddSingleton<IPhilosopherStrategy, HierarchyStrategy>();

                    // Менеджер стола (вилки, философы)
                    services.AddSingleton<ITableManager, TableManager>();

                    // Метрики
                    services.AddSingleton<IMultithreadedMetricsCollector>(sp =>
                    {
                        var table = sp.GetRequiredService<ITableManager>();
                        return new MultithreadedMetricsCollector(table.GetPhilosophers(), table.GetForks());
                    });
                    services.AddSingleton<IMetricsCollector>(sp =>
                    {
                        var multiMetrics = sp.GetRequiredService<IMultithreadedMetricsCollector>();
                        return new MultithreadedToMetricsAdapter(multiMetrics);
                    });

                    // Монитор
                    services.AddSingleton<IMonitor>(sp =>
                    {
                        var config = sp.GetRequiredService<IOptions<MultithreadedSimulationConfig>>().Value;
                        return new MultithreadedConsoleMonitor(config.DisplayInterval);
                    });

                    // Менеджер приобретения вилок
                    services.AddSingleton<ThreadSafeForkAcquisitionManager>(sp =>
                    {
                        var config = sp.GetRequiredService<IOptions<MultithreadedSimulationConfig>>().Value;
                        return new ThreadSafeForkAcquisitionManager(config.ForkAcquisitionTime);
                    });

                    // Сервис обновления метрик
                    services.AddHostedService<MetricsUpdateService>();

                    // Сервис отображения
                    services.AddHostedService<DisplayService>();

                    // Сервис проверки дедлока
                    services.AddHostedService<DeadlockCheckerService>();

                    // Регистрируем сервисы для каждого философа
                    services.AddHostedService<Plato>();
                    services.AddHostedService<Aristotle>();
                    services.AddHostedService<Socrates>();
                    services.AddHostedService<Descartes>();
                    services.AddHostedService<Kant>();
                });
    }
}