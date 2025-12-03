using System;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Определяем тип симуляции через аргументы командной строки
                var simulationType = args.Length > 0 && args[0] == "multithreaded" 
                    ? SimulationType.Multithreaded 
                    : SimulationType.StepByStep;

                if (simulationType == SimulationType.Multithreaded)
                {
                    await SimulationLauncher.RunMultithreadedSimulationAsync();
                }
                else
                {
                    SimulationLauncher.RunStepByStepSimulation();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            Console.WriteLine("Simulation finished. Press any key to exit...");
            Console.ReadKey();
        }
    }
}