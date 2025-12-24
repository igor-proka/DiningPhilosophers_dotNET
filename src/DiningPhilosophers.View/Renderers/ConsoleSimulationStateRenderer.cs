using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Persistence.Entities;
using DiningPhilosophers.View.CommandLine;

namespace DiningPhilosophers.View.Renderers
{
    public class ConsoleSimulationStateRenderer : ISimulationStateRenderer
    {
        public void Render(CommandLineArguments arguments, SimulationRun run,
            IEnumerable<PhilosopherStateEvent> philosophers, IEnumerable<ForkStateEvent> forks,
            string? warningMessage = null)
        {
            if (run == null)
            {
                Console.WriteLine("Simulation run not found.");
                return;
            }

            var requestedTime = arguments.RequestedTimeUtc;
            var localTime = requestedTime.ToLocalTime();
            var simulationDuration = run.FinishedAtUtc.HasValue 
                ? (run.FinishedAtUtc.Value - run.StartedAtUtc).TotalSeconds
                : (DateTime.UtcNow - run.StartedAtUtc).TotalSeconds;
            
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║               SIMULATION STATE VIEWER                    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            
            Console.WriteLine("📊 SIMULATION INFO:");
            Console.WriteLine($"   Run ID:           {arguments.RunId}");
            Console.WriteLine($"   Start time:       {run.StartedAtUtc:yyyy-MM-dd HH:mm:ss.fff} UTC");
            Console.WriteLine($"   Status:           {(run.FinishedAtUtc.HasValue ? "Completed" : "Running")}");
            if (run.FinishedAtUtc.HasValue)
            {
                Console.WriteLine($"   End time:         {run.FinishedAtUtc.Value:yyyy-MM-dd HH:mm:ss.fff} UTC");
                Console.WriteLine($"   Duration:         {simulationDuration:F2} seconds");
            }
            Console.WriteLine();
            
            Console.WriteLine("📅 REQUESTED TIME:");
            Console.WriteLine($"   Delay from start: {arguments.DelaySeconds:F2} seconds");
            Console.WriteLine($"   Requested time:   {requestedTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
            Console.WriteLine($"                     {localTime:yyyy-MM-dd HH:mm:ss.fff} Local");
            Console.WriteLine();
            
            if (!string.IsNullOrEmpty(warningMessage))
            {
                Console.WriteLine("⚠️  WARNING:");
                Console.WriteLine($"   {warningMessage}");
                Console.WriteLine();
            }
            
            Console.WriteLine("👥 PHILOSOPHERS:");
            Console.WriteLine("   ┌─────────────────────────────────────────────────────┐");
            foreach (var p in philosophers.OrderBy(p => p.PhilosopherName))
            {
                RenderPhilosopher(p, run.StartedAtUtc);
            }
            Console.WriteLine("   └─────────────────────────────────────────────────────┘");
            Console.WriteLine();
            
            Console.WriteLine("🍴 FORKS:");
            Console.WriteLine("   ┌─────────────────────────────────────────────────────┐");
            foreach (var f in forks.OrderBy(f => f.ForkNumber))
            {
                RenderFork(f, run.StartedAtUtc);
            }
            Console.WriteLine("   └─────────────────────────────────────────────────────┘");
        }
        
        private void RenderPhilosopher(PhilosopherStateEvent philosopher, DateTime simulationStart)
        {
            var stateIcon = GetStateIcon(philosopher.State);
            var name = $"{philosopher.PhilosopherName,-10}";
            var state = $"{stateIcon} {philosopher.State,-8}";
            
            var details = new List<string>();
            
            if (!string.IsNullOrEmpty(philosopher.CurrentAction) && philosopher.CurrentAction != "None")
                details.Add($"Action: {philosopher.CurrentAction}");
                
            if (philosopher.StepsRemaining.HasValue && philosopher.StepsRemaining > 0)
                details.Add($"{philosopher.StepsRemaining}ms left");
                                
            var detailsStr = details.Count > 0 ? $" ({string.Join(", ", details)})" : "";
            var lastSeen = FormatTimestamp(philosopher.TimestampUtc, simulationStart);
            
            Console.WriteLine($"   │ {name} {state}{detailsStr,-30} [⏰ {lastSeen}] │");
        }
        
        private void RenderFork(ForkStateEvent fork, DateTime simulationStart)
        {
            var stateIcon = fork.State == "InUse" ? "🔴" : "🟢";
            var owner = string.IsNullOrEmpty(fork.Owner) ? "Available" : $"Used by {fork.Owner}";
            var lastSeen = FormatTimestamp(fork.TimestampUtc, simulationStart);
            
            Console.WriteLine($"   │ Fork-{fork.ForkNumber} {stateIcon} {owner,-25} [⏰ {lastSeen}] │");
        }
        
        private string GetStateIcon(string state)
        {
            return state switch
            {
                "Thinking" => "💭",
                "Hungry" => "😋",
                "Eating" => "🍝",
                _ => "❓"
            };
        }
        
        private string FormatTimestamp(DateTime timestamp, DateTime simulationStart)
        {
            var timeSinceStart = timestamp - simulationStart;
            
            if (timestamp.Date == simulationStart.Date)
            {
                // В тот же день: показываем только время
                return $"{timestamp:HH:mm:ss.fff} (+{timeSinceStart.TotalSeconds:F2}s)";
            }
            else
            {
                // В другой день: показываем дату и время
                return $"{timestamp:MM-dd HH:mm:ss} (+{timeSinceStart.TotalSeconds:F2}s)";
            }
        }
    }
}