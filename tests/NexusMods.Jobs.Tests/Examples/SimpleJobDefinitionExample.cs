using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
namespace NexusMods.Jobs.Tests.Examples;

// Simple job definitions (using the `Begin` method with a lambda) are used when you
// want to handle the execution logic in the same method where you call `Begin`.

// This can be useful when there's not much code to be executed, but you still
// want to offload the work to the job system.

[PublicAPI]
public class SimpleJobDefinitionExample(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task RunExample()
    {
        var jobDefinition = new MySimpleJobDefinition { InputData = "test" };

        var jobTask = jobMonitor.Begin(jobDefinition, async (context) =>
        {
            // Access job data through context
            var data = context.Definition.InputData;

            // Report progress
            context.SetPercent(Size.Zero, Size.From(100));

            // Do some work with the data
            await Task.Delay(100, context.CancellationToken);
            context.SetPercent(Size.From(50), Size.From(100));

            // Call YieldAsync() around expensive operations
            // This handles job cancellation and helps task scheduling
            await context.YieldAsync();

            // Complete work
            await Task.Delay(100, context.CancellationToken);
            context.SetPercent(Size.From(100), Size.From(100));

            return $"Job completed with: {data}";
        });

        // Wait for result
        var result = await jobTask;
        result.Should().Be("Job completed with: test");
    }
}

// Use records, as jobs should not mutate their own parameters.
public record MySimpleJobDefinition : IJobDefinition<string>
{
    public string InputData { get; init; } = "";
}
