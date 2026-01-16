using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using MassTransit;
using Microservices.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

public class HungryConsumer : IConsumer<HungryEvent>
{
    private readonly ICoordinator _coordinator;
    private readonly IReadOnlyDictionary<string, Philosopher> _philosophers;
    private readonly ILogger<HungryConsumer> _logger;

    public HungryConsumer(
        ICoordinator coordinator, 
        IReadOnlyDictionary<string, Philosopher> philosophers,
        ILogger<HungryConsumer> logger)
    {
        _coordinator = coordinator;
        _philosophers = philosophers;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<HungryEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received HungryEvent from {Philosopher}", message.PhilosopherName);

        if (!_philosophers.TryGetValue(message.PhilosopherName, out var philosopher))
        {
            _logger.LogWarning("Philosopher {Name} not found", message.PhilosopherName);
            return Task.CompletedTask;
        }

        // Уведомляем координатора
        _coordinator.NotifyHungry(philosopher);
        
        // Подписываемся на решение координатора
        _coordinator.DecisionEvent += async (phil, action) =>
        {
            if (phil.Name == philosopher.Name)
            {
                _logger.LogInformation("Publishing decision for {Philosopher}: {Action}", phil.Name, action);
                await context.Publish(new PhilosopherDecision
                {
                    PhilosopherName = phil.Name,
                    Action = action
                });
            }
        };

        return Task.CompletedTask;
    }
}