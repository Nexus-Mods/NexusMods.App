// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
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
        if (int.TryParse(arg, out var result) && (result >= 0 && result < benchmarks.Length))
            StartBenchmark(benchmarks, result);
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
    if (int.TryParse(line, out var result))
    {
        if (result >= 0 && result < benchmarks.Length)
            StartBenchmark(benchmarks, result);
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

void StartBenchmark(SelectableBenchmark[] selectableBenchmarks, int benchmarkIndex)
{
    if (Debugger.IsAttached)
    {
        BenchmarkRunner.Run(selectableBenchmarks[benchmarkIndex].Type, new DebugInProcessConfig());
    }
    else
    {
        // Ensure our parameters don't get trimmed.
        var summaryStyle = new SummaryStyle(CultureInfo.CurrentCulture, printUnitsInHeader: false, printUnitsInContent: true,
            printZeroValuesInContent: false, sizeUnit: null, timeUnit: null, maxParameterColumnWidth: 40);
        var config = ManualConfig.Create(DefaultConfig.Instance).WithSummaryStyle(summaryStyle);
        BenchmarkRunner.Run(selectableBenchmarks[benchmarkIndex].Type, config);
    }
}
