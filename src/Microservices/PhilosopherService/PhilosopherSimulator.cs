using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using MassTransit;
using Microservices.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class PhilosopherSimulator : BackgroundService
{
    private readonly MultithreadedPhilosopherStateProcessor _processor;
    private readonly IBus _bus;
    private readonly HttpClient _httpClient;
    private readonly MultithreadedSimulationConfig _config;
    private readonly ILogger<PhilosopherSimulator> _logger;
    private readonly ThreadSafeForkAcquisitionManager _acquisitionManager;
    private readonly Random _random = new Random();

    public PhilosopherSimulator(
        MultithreadedPhilosopherStateProcessor processor,
        IBus bus,
        IHttpClientFactory clientFactory,
        IOptions<MultithreadedSimulationConfig> configOptions,
        ILogger<PhilosopherSimulator> logger,
        ThreadSafeForkAcquisitionManager acquisitionManager)
    {
        _processor = processor;
        _bus = bus;
        _httpClient = clientFactory.CreateClient("Table");
        _config = configOptions.Value;
        _logger = logger;
        _acquisitionManager = acquisitionManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting philosopher {Name}", _processor.Philosopher.Name);

        try
        {
            var startTime = DateTime.Now;
            var endTime = startTime.AddSeconds(_config.DurationSeconds);

            while (!stoppingToken.IsCancellationRequested && DateTime.Now < endTime)
            {
                // Основной цикл философа
                switch (_processor.Philosopher.State)
                {
                    case PhilosopherState.Thinking:
                        await ProcessThinkingState(stoppingToken);
                        break;
                    case PhilosopherState.Hungry:
                        await ProcessHungryState(stoppingToken);
                        break;
                    case PhilosopherState.Eating:
                        await ProcessEatingState(stoppingToken);
                        break;
                }

                await Task.Delay(10, stoppingToken);
            }

            // Симуляция завершена - отправляем метрики
            await SendFinalMetrics(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Philosopher {Name} simulation cancelled", _processor.Philosopher.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in philosopher {Name} simulation", _processor.Philosopher.Name);
        }
    }

    private async Task ProcessThinkingState(CancellationToken ct)
    {
        var thinkingTime = _random.Next(_config.ThinkingTimeMin, _config.ThinkingTimeMax + 1);
        await Task.Delay(thinkingTime, ct);
        
        _processor.Philosopher.State = PhilosopherState.Hungry;
        _processor.Philosopher.CurrentAction = PhilosopherAction.None;
        
        // Начинаем отсчет ожидания
        _processor.Metrics.StartWaiting(_processor.Philosopher.Name);
        
        // Публикуем событие голода
        await _bus.Publish(new HungryEvent
        {
            PhilosopherName = _processor.Philosopher.Name,
            LeftForkId = _processor.LeftFork.Id.ToString(),
            RightForkId = _processor.RightFork.Id.ToString()
        }, ct);
        
        _logger.LogInformation("{Name} became hungry", _processor.Philosopher.Name);
    }

    private async Task ProcessHungryState(CancellationToken ct)
    {
        // Ждем решения координатора или используем локальную стратегию
        var action = _processor.Philosopher.CurrentAction;
        
        if (action != PhilosopherAction.None)
        {
            // Обрабатываем действие от координатора
            await ProcessActionFromCoordinator(action, ct);
            
            // Проверяем, можем ли начать есть
            if (_processor.Philosopher.HasLeftFork && _processor.Philosopher.HasRightFork)
            {
                // Заканчиваем отсчет ожидания
                _processor.Metrics.StopWaiting(_processor.Philosopher.Name);
                await StartEating(ct);
            }
        }
        else
        {
            // Если нет действия от координатора, ждем немного
            await Task.Delay(10, ct);
        }
        
        // Сбрасываем действие для следующего цикла
        _processor.Philosopher.CurrentAction = PhilosopherAction.None;
    }

    private async Task ProcessActionFromCoordinator(PhilosopherAction action, CancellationToken ct)
    {
        // Взятие левой вилки
        if (action.HasFlag(PhilosopherAction.TakeLeftFork) && !_processor.Philosopher.HasLeftFork)
        {
            var request = new ForkRequest 
            { 
                ForkId = _processor.LeftFork.Id.ToString(), 
                Action = ForkAction.Take, 
                PhilosopherName = _processor.Philosopher.Name 
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/forks/action", request, ct);
            if (response.IsSuccessStatusCode)
            {
                await Task.Delay(_config.ForkAcquisitionTime, ct); // Имитация времени взятия
                _processor.Philosopher.HasLeftFork = true;
                _logger.LogInformation("{Name} acquired left fork {ForkId}", 
                    _processor.Philosopher.Name, _processor.LeftFork.Id);
            }
        }

        // Взятие правой вилки
        if (action.HasFlag(PhilosopherAction.TakeRightFork) && !_processor.Philosopher.HasRightFork)
        {
            var request = new ForkRequest 
            { 
                ForkId = _processor.RightFork.Id.ToString(), 
                Action = ForkAction.Take, 
                PhilosopherName = _processor.Philosopher.Name 
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/forks/action", request, ct);
            if (response.IsSuccessStatusCode)
            {
                await Task.Delay(_config.ForkAcquisitionTime, ct);
                _processor.Philosopher.HasRightFork = true;
                _logger.LogInformation("{Name} acquired right fork {ForkId}", 
                    _processor.Philosopher.Name, _processor.RightFork.Id);
            }
        }

        // Освобождение левой вилки
        if (action.HasFlag(PhilosopherAction.ReleaseLeftFork) && _processor.Philosopher.HasLeftFork)
        {
            var request = new ForkRequest 
            { 
                ForkId = _processor.LeftFork.Id.ToString(), 
                Action = ForkAction.Release, 
                PhilosopherName = _processor.Philosopher.Name 
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/forks/action", request, ct);
            if (response.IsSuccessStatusCode)
            {
                _processor.Philosopher.HasLeftFork = false;
                _acquisitionManager.ResetProgress(_processor.Philosopher);
                _logger.LogInformation("{Name} released left fork {ForkId}", 
                    _processor.Philosopher.Name, _processor.LeftFork.Id);
            }
        }

        // Освобождение правой вилки
        if (action.HasFlag(PhilosopherAction.ReleaseRightFork) && _processor.Philosopher.HasRightFork)
        {
            var request = new ForkRequest 
            { 
                ForkId = _processor.RightFork.Id.ToString(), 
                Action = ForkAction.Release, 
                PhilosopherName = _processor.Philosopher.Name 
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/forks/action", request, ct);
            if (response.IsSuccessStatusCode)
            {
                _processor.Philosopher.HasRightFork = false;
                _acquisitionManager.ResetProgress(_processor.Philosopher);
                _logger.LogInformation("{Name} released right fork {ForkId}", 
                    _processor.Philosopher.Name, _processor.RightFork.Id);
            }
        }
    }

    private async Task StartEating(CancellationToken ct)
    {
        _processor.Philosopher.State = PhilosopherState.Eating;
        var eatingTime = _random.Next(_config.EatingTimeMin, _config.EatingTimeMax + 1);
        _processor.Philosopher.StepsRemaining = eatingTime;
        
        // Увеличиваем счетчик съеденного
        _processor.Metrics.IncrementMeal(_processor.Philosopher.Name);
        
        _logger.LogInformation("{Name} started eating for {Time}ms", 
            _processor.Philosopher.Name, eatingTime);
    }

    private async Task ProcessEatingState(CancellationToken ct)
    {
        await Task.Delay(_processor.Philosopher.StepsRemaining, ct);
        
        // Освобождаем вилки
        await ReleaseForks(ct);
        
        // Переходим в состояние размышлений
        _processor.Philosopher.State = PhilosopherState.Thinking;
        var thinkingTime = _random.Next(_config.ThinkingTimeMin, _config.ThinkingTimeMax + 1);
        _processor.Philosopher.StepsRemaining = thinkingTime;
        _processor.Philosopher.CurrentAction = PhilosopherAction.None;
        
        _logger.LogInformation("{Name} finished eating and started thinking", _processor.Philosopher.Name);
    }

    private async Task ReleaseForks(CancellationToken ct)
    {
        if (_processor.Philosopher.HasLeftFork)
        {
            var request = new ForkRequest 
            { 
                ForkId = _processor.LeftFork.Id.ToString(), 
                Action = ForkAction.Release, 
                PhilosopherName = _processor.Philosopher.Name 
            };
            await _httpClient.PostAsJsonAsync("api/forks/action", request, ct);
            _processor.Philosopher.HasLeftFork = false;
        }

        if (_processor.Philosopher.HasRightFork)
        {
            var request = new ForkRequest 
            { 
                ForkId = _processor.RightFork.Id.ToString(), 
                Action = ForkAction.Release, 
                PhilosopherName = _processor.Philosopher.Name 
            };
            await _httpClient.PostAsJsonAsync("api/forks/action", request, ct);
            _processor.Philosopher.HasRightFork = false;
        }
    }

    private async Task SendFinalMetrics(CancellationToken ct)
    {
        var metrics = _processor.Metrics.GetPhilosopherMetrics(_processor.Philosopher.Name);
        
        var metricsRequest = new MetricsRequest 
        { 
            PhilosopherName = _processor.Philosopher.Name,
            MealsEaten = metrics.MealsEaten,
            TotalWaitingTimeMs = metrics.TotalWaitingTimeMs,
            HungerEpisodes = metrics.HungerEpisodes
        };
        
        var response = await _httpClient.PostAsJsonAsync("api/metrics/submit", metricsRequest, ct);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("{Name} sent final metrics: {Meals} meals", 
                _processor.Philosopher.Name, metrics.MealsEaten);
        }
        else
        {
            _logger.LogWarning("{Name} failed to send metrics", _processor.Philosopher.Name);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping philosopher {Name}", _processor.Philosopher.Name);
        await base.StopAsync(cancellationToken);
    }
}