using System;
using System.Globalization;

namespace DiningPhilosophers.View.CommandLine
{
    public class CommandLineParser
    {
        public CommandLineArguments Parse(string[] args)
        {
            var arguments = new CommandLineArguments();
            
            if (args == null || args.Length == 0)
                return arguments;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals("--runId", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    Guid.TryParse(args[i + 1], out var runId);
                    arguments.RunId = runId;
                    i++;
                }
                else if (arg.Equals("--delay", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var delay);
                    arguments.DelaySeconds = delay;
                    i++;
                }
            }

            return arguments;
        }

        public void DisplayUsage()
        {
            Console.WriteLine("Usage: DiningPhilosophers.View --runId <guid> --delay <seconds>");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  --runId    Unique identifier of the simulation run");
            Console.WriteLine("  --delay    Time offset in seconds from the start of simulation");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  DiningPhilosophers.View --runId 0537ac6e-2f6b-41f5-9690-92b013eb9c0f --delay 5.5");
        }
    }
}