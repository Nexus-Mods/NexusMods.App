// See https://aka.ms/new-console-template for more information

using System.Reflection;
using BenchmarkDotNet.Running;
using NexusMods.Benchmarks;
using NexusMods.Benchmarks.Interfaces;

var benchmarks = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(x => x.IsAssignableTo(typeof(IBenchmark)) && x != typeof(IBenchmark))
    .Select(x => new SelectableBenchmark(x))
    .ToArray();

Console.WriteLine("Note: Benchmarks can be ran directly by passing arguments in CLI, e.g. NexusMods.Benchmarks.dll 0 1\n");
if (args.Length > 0)
{
    foreach (var arg in args)
    {
        if (int.TryParse(arg, out int result) && (result >= 0 && result < benchmarks.Length))
            BenchmarkRunner.Run(benchmarks[result].Type);
    }

    return;
}

while (true)
{
    Console.WriteLine("Select a Benchmark");
    
    var origColour = Console.ForegroundColor;
    
    Console.ForegroundColor = ConsoleColor.Green;
    for (var x = 0; x < benchmarks.Length; x++)
        PrintOption(x, benchmarks[x]);

    Console.ForegroundColor = origColour;
    Console.WriteLine("\nEnter any invalid number to exit.");

    var line = Console.ReadLine();
    if (int.TryParse(line, out int result))
    {
        if (result >= 0 && result < benchmarks.Length)
            BenchmarkRunner.Run(benchmarks[result].Type);
        else
            return;
    }
}

void PrintOption(int x, SelectableBenchmark benchmark)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"{x}. {benchmark.Name}: ");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(benchmark.Description);
}