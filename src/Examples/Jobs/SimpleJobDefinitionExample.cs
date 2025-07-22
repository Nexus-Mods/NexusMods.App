using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
namespace Examples.Jobs;

// Simple job definitions are used when you want to handle the execution logic
// in the same method where you call Begin(). The job definition serves as context/data,
// while the actual work is done in the lambda callback right where you need it.

// Use records, as jobs should not mutate their own parameters.
public record MyJobDefinition : IJobDefinition<string>
{
    public string InputData { get; init; } = "";
}

[PublicAPI]
public static class SimpleJobDefinitionExample
{
    public static async Task<string> RunExample(IServiceProvider serviceProvider)
    {
        var jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        var jobDefinition = new MyJobDefinition { InputData = "test" };

        var jobTask = jobMonitor.Begin(jobDefinition, async (context) =>
        {
            // Access job data through context
            var data = context.Definition.InputData;

            // Report progress
            context.SetPercent(Size.Zero, Size.From(100));

            // Do some work with the data
            await Task.Delay(1000, context.CancellationToken);
            context.SetPercent(Size.From(50), Size.From(100));

            // Call YieldAsync() around expensive operations
            // This handles job cancellation and helps task scheduling
            await context.YieldAsync();

            // Complete work
            await Task.Delay(1000, context.CancellationToken);
            context.SetPercent(Size.From(100), Size.From(100));

            return $"Job completed with: {data}";
        });

        // Wait for result
        return await jobTask;
    }
}
