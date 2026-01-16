using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using Microservices.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class ForksController : ControllerBase
{
    private readonly ITableManager _tableManager;
    private readonly ILogger<ForksController> _logger;

    public ForksController(ITableManager tableManager, ILogger<ForksController> logger)
    {
        _tableManager = tableManager;
        _logger = logger;
    }

    [HttpPost("action")]
    public IActionResult PerformForkAction([FromBody] ForkRequest request)
    {
        _logger.LogInformation("Fork action: {Action} on fork {ForkId} by {Philosopher}", 
            request.Action, request.ForkId, request.PhilosopherName);

        var forks = _tableManager.GetForks();
        var fork = forks.FirstOrDefault(f => f.Id.ToString() == request.ForkId);
        if (fork == null)
        {
            _logger.LogWarning("Fork {ForkId} not found", request.ForkId);
            return NotFound(new { Success = false, Message = $"Fork {request.ForkId} not found" });
        }

        if (request.Action == ForkAction.Take)
        {
            if (fork.TryAcquire(request.PhilosopherName))
            {
                _logger.LogInformation("Fork {ForkId} acquired by {Philosopher}", request.ForkId, request.PhilosopherName);
                return Ok(new { Success = true, Message = $"Fork {request.ForkId} acquired by {request.PhilosopherName}" });
            }
            _logger.LogInformation("Fork {ForkId} is already in use", request.ForkId);
            return Conflict(new { Success = false, Message = $"Fork {request.ForkId} is already in use" });
        }
        else
        {
            fork.Release();
            _logger.LogInformation("Fork {ForkId} released", request.ForkId);
            return Ok(new { Success = true, Message = $"Fork {request.ForkId} released" });
        }
    }

    [HttpGet]
    public IActionResult GetForks()
    {
        var forks = _tableManager.GetForks();
        return Ok(forks.Select(f => new 
        { 
            Id = f.Id, 
            State = f.State.ToString(), 
            Owner = f.Owner 
        }));
    }

    [HttpGet("{id}")]
    public IActionResult GetFork(int id)
    {
        var forks = _tableManager.GetForks();
        var fork = forks.FirstOrDefault(f => f.Id == id);
        if (fork == null) return NotFound();
        
        return Ok(new 
        { 
            Id = fork.Id, 
            State = fork.State.ToString(), 
            Owner = fork.Owner 
        });
    }
}