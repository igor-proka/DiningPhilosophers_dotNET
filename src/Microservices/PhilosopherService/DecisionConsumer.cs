using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using MassTransit;
using Microservices.Shared;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class DecisionConsumer : IConsumer<PhilosopherDecision>
{
    private readonly MultithreadedPhilosopherStateProcessor _processor;
    private readonly ILogger<DecisionConsumer> _logger;

    public DecisionConsumer(
        MultithreadedPhilosopherStateProcessor processor,
        ILogger<DecisionConsumer> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<PhilosopherDecision> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received decision for {Philosopher}: {Action}", 
            message.PhilosopherName, message.Action);

        // Устанавливаем действие для философа
        _processor.Philosopher.CurrentAction = message.Action;
        return Task.CompletedTask;
    }
}