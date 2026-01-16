using DiningPhilosophers.Core.Contracts.Monitor;
using Microservices.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IMultithreadedMetricsCollector _metrics;
    private static readonly ConcurrentDictionary<string, MetricsRequest> _philosopherMetrics = new();
    private static int _exitedPhilosophers = 0;
    private readonly int _totalPhilosophers = 5;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IMultithreadedMetricsCollector metrics,
        ILogger<MetricsController> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    [HttpPost("submit")]
    public IActionResult SubmitMetrics([FromBody] MetricsRequest request)
    {
        _logger.LogInformation("Metrics received from {Philosopher}: {Meals} meals, {WaitTime}ms waiting", 
            request.PhilosopherName, request.MealsEaten, request.TotalWaitingTimeMs);

        _philosopherMetrics[request.PhilosopherName] = request;

        _exitedPhilosophers++;
        _logger.LogInformation("Philosopher {Name} exited. Total exited: {Exited}/{Total}", 
            request.PhilosopherName, _exitedPhilosophers, _totalPhilosophers);

        if (_exitedPhilosophers == _totalPhilosophers)
        {
            PrintFinalSummary();
        }

        return Ok(new { Success = true, Message = $"Metrics for {request.PhilosopherName} received" });
    }

    [HttpGet]
    public IActionResult GetMetrics()
    {
        return Ok(_philosopherMetrics);
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        if (_exitedPhilosophers < _totalPhilosophers)
        {
            return Ok(new { 
                Status = "Running", 
                ExitedPhilosophers = _exitedPhilosophers, 
                TotalPhilosophers = _totalPhilosophers 
            });
        }

        var summary = new
        {
            Status = "Completed",
            TotalPhilosophers = _totalPhilosophers,
            Philosophers = _philosopherMetrics.Select(kv => new
            {
                Name = kv.Key,
                MealsEaten = kv.Value.MealsEaten,
                TotalWaitingTimeMs = kv.Value.TotalWaitingTimeMs,
                HungerEpisodes = kv.Value.HungerEpisodes
            }).ToList(),
            TotalMeals = _philosopherMetrics.Sum(kv => kv.Value.MealsEaten),
            AverageWaitingTimeMs = _philosopherMetrics.Average(kv => kv.Value.TotalWaitingTimeMs)
        };

        return Ok(summary);
    }

    private void PrintFinalSummary()
    {
        _logger.LogInformation(new string('=', 60));
        _logger.LogInformation("ВСЕ ФИЛОСОФЫ ЗАВЕРШИЛИ СИМУЛЯЦИЮ");
        _logger.LogInformation(new string('=', 60));
        
        foreach (var kv in _philosopherMetrics.OrderBy(k => k.Key))
        {
            _logger.LogInformation("{Name}: Съедено {Meals} раз, " +
                                "Ожидание: {WaitTime}мс, " +
                                "Эпизодов голода: {Episodes}",
                                kv.Key, kv.Value.MealsEaten,
                                kv.Value.TotalWaitingTimeMs,
                                kv.Value.HungerEpisodes);
        }
        
        var totalMeals = _philosopherMetrics.Sum(kv => kv.Value.MealsEaten);
        var avgWait = _philosopherMetrics.Average(kv => kv.Value.TotalWaitingTimeMs);
        
        _logger.LogInformation("\nИТОГО: Всего съедено {TotalMeals} раз, Среднее ожидание: {AvgWait:F0}мс",
                               totalMeals, avgWait);
    }
}