using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using DiningPhilosophers.Persistence;

namespace DiningPhilosophers.Hosted.Services.Philosophers
{
    public class PhilosopherHostedService : BackgroundService
    {
        private readonly MultithreadedPhilosopherStateProcessor? _processor;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _philosopherName;

        public PhilosopherHostedService(
            ITableManager tableManager,
            IOptions<MultithreadedSimulationConfig> configOptions,
            IPhilosopherStrategy strategy,
            ThreadSafeForkAcquisitionManager acquisitionManager,
            IMultithreadedMetricsCollector metrics,
            string philosopherName,
            IServiceProvider serviceProvider)
        {
            _philosopherName = philosopherName;
            var philosopher = tableManager.GetPhilosopher(philosopherName);
            var leftFork = tableManager.GetLeftFork(philosopherName);
            var rightFork = tableManager.GetRightFork(philosopherName);
            var config = configOptions.Value;

            if (philosopher == null)
                throw new ArgumentException($"Philosopher {philosopherName} not found in table manager");

            _processor = new MultithreadedPhilosopherStateProcessor(
                philosopher, leftFork, rightFork, config, strategy, acquisitionManager, metrics);

            _serviceProvider = serviceProvider;

            // Лог
            Console.WriteLine($"Creating PhilosopherHostedService for {philosopherName} -> table philosopher name: {philosopher?.Name}");
        }

        // Переопределение StartAsync для установки сохранения состояния в процессоре
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // Лог — покажем, с каким философом работаем
            Console.WriteLine($"PhilosopherHostedService.StartAsync called for {_processor?.Name ?? "UNKNOWN"}");

            // Получаем зависимости (лениво, т.к. в конструкторе не всегда есть runContext)
            var persistence = _serviceProvider.GetService<ISimulationPersistence>();
            var runContext = _serviceProvider.GetService<RunContext>();

            // Ждём инициализации runContext (если он есть) — не делаем immediate return!
            if (runContext != null)
            {
                var waitDelayMs = 1;
                while (!runContext.IsInitialized && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(waitDelayMs, cancellationToken);
                }
            }

            // Если persistence и runContext готовы — устанавливаем в процессор
            if (persistence != null && runContext != null && runContext.IsInitialized)
            {
                // ВАЖНО: Добавляем проверку на null для _processor
                if (_processor != null)
                {
                    _processor.SetPersistence(persistence, runContext);
                    Console.WriteLine($"PhilosopherHostedService for {_philosopherName}: persistence set, runId = {runContext.RunId}");
                }
                else
                {
                    Console.WriteLine($"PhilosopherHostedService for {_philosopherName}: Processor is null, cannot set persistence");
                }
            }
            else
            {
                Console.WriteLine($"PhilosopherHostedService for {_philosopherName}: persistence NOT set (will run without DB writes)");
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Запускаем основной цикл процессора (внутри RunAsync уже бесконечный цикл)
                if (_processor != null)
                {
                    await _processor.RunAsync(stoppingToken);
                }
                else
                {
                    Console.WriteLine($"PhilosopherHostedService for {_philosopherName}: Processor is null, cannot run");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Нормальное завершение по токену - игнорируем
                _ = Console.Out; // не выполнять никаких действий, чтобы избежать предупреждений.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PhilosopherHostedService for {_philosopherName} crashed: {ex}");
                throw;
            }
        }
    }
}