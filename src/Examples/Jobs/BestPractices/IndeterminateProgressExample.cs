using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
// ReSharper disable LocalizableElement
namespace Examples.Jobs.BestPractices;

// For indeterminate progress (unknown total work), use Size.One as maximum to avoid division by zero.

[PublicAPI]
public class IndeterminateProgressExample(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task DemonstrateIndeterminateProgress()
    {
        // For jobs where total work is unknown, use Size.One to avoid division by zero
        var jobDefinition = new MyJobDefinition { InputData = "processing unknown amount" };

        var jobTask = jobMonitor.Begin(jobDefinition, async (context) =>
        {
            var processed = 0;

            // When we don't know the total, use Size.One as max to show relative progress
            context.SetPercent(Size.Zero, Size.One);

            // Process items as they come
            for (var x = 0; x < 10; x++)
            {
                await Task.Delay(10, context.CancellationToken);
                processed++;

                // Show progress as fraction of known work so far
                context.SetPercent(Size.From((ulong)processed), Size.From((ulong)(processed + 1)));

                await context.YieldAsync();
            }

            // Final progress when complete
            context.SetPercent(Size.One, Size.One);
            return processed;
        });

        // Result
        var result = await jobTask; // Item count.
        result.Should().Be(10);
    }
}

// Helper class from the simple example
public record MyJobDefinition : IJobDefinition<int> // 👈 return type
{
    public string InputData { get; init; } = "";
}
