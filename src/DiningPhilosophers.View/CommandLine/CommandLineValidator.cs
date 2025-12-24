using System;

namespace DiningPhilosophers.View.CommandLine
{
    public class CommandLineValidator
    {
        public (bool IsValid, string? ErrorMessage) Validate(CommandLineArguments arguments)
        {
            if (arguments.RunId == Guid.Empty)
                return (false, "RunId is required.");
                
            if (arguments.DelaySeconds < 0)
                return (false, "Delay cannot be negative.");
                
            return (true, null);
        }
        
        public (bool IsWithinSimulation, string? WarningMessage) ValidateDelayAgainstSimulation(
            CommandLineArguments arguments, DateTime startedAt, DateTime? finishedAt)
        {
            var requestedTime = startedAt.AddSeconds(arguments.DelaySeconds);
            var simulationEnd = finishedAt ?? DateTime.UtcNow;
            
            if (requestedTime < startedAt)
            {
                return (false, $"Requested time ({requestedTime:HH:mm:ss.fff}) is before simulation start ({startedAt:HH:mm:ss.fff}).");
            }
            
            if (requestedTime > simulationEnd)
            {
                var actualDuration = (simulationEnd - startedAt).TotalSeconds;
                return (false, 
                    $"Requested time ({arguments.DelaySeconds:F2}s) exceeds simulation duration ({actualDuration:F2}s). " +
                    $"Showing state at simulation end ({actualDuration:F2}s).");
            }
            
            return (true, null);
        }
    }
}