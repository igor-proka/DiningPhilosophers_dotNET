using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public interface ICoordinatorService
{
    Task HandlePhilosopherHungry(string philosopherName);
    Task HandlePhilosopherFinished(string philosopherName);
}

public class CoordinatorServiceImpl : ICoordinatorService
{
    private readonly ILogger<CoordinatorServiceImpl> _logger;

    public CoordinatorServiceImpl(ILogger<CoordinatorServiceImpl> logger)
    {
        _logger = logger;
    }

    public Task HandlePhilosopherHungry(string philosopherName)
    {
        _logger.LogInformation("Philosopher {Name} is hungry", philosopherName);
        return Task.CompletedTask;
    }

    public Task HandlePhilosopherFinished(string philosopherName)
    {
        _logger.LogInformation("Philosopher {Name} finished eating", philosopherName);
        return Task.CompletedTask;
    }
}