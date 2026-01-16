using DiningPhilosophers.Core.Contracts.Strategies;
using MassTransit;
using Microservices.Shared;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class FinishedConsumer : IConsumer<FinishedEvent>
{
    private readonly ICoordinator _coordinator;
    private readonly ILogger<FinishedConsumer> _logger;

    public FinishedConsumer(ICoordinator coordinator, ILogger<FinishedConsumer> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<FinishedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Philosopher {Name} finished eating", message.PhilosopherName);
        
        return Task.CompletedTask;
    }
}