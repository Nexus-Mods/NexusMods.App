using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
// ReSharper disable LocalizableElement

namespace NexusMods.Jobs.Tests.Examples.BestPractices;

[PublicAPI]
public class InteractiveCancellationExample(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task DemonstrateSelfCancellation()
    {
        var job = new InteractiveInstallJob { ItemName = "TestMod" };
        var jobTask = jobMonitor.Begin<InteractiveInstallJob, bool>(job);

        try
        {
            var result = await jobTask;
            // Most likely outcome (80% chance in simulation)
            result.Should().BeTrue();
        }
        catch (OperationCanceledException)
        {
            // Also valid outcome (20% chance in simulation)
            true.Should().BeTrue("Job was cancelled as expected");
        }
    }
}

public record InteractiveInstallJob : IJobDefinitionWithStart<InteractiveInstallJob, bool>
{
    public required string ItemName { get; init; }

    public async ValueTask<bool> StartAsync(IJobContext<InteractiveInstallJob> context)
    {
        try
        {
            // Simulate interaction with an external component that may cancel
            var shouldInstall = await SimulateUserInteraction();
            
            // Self-cancellation: The job cancels itself based on user input
            if (!shouldInstall)
                context.CancelAndThrow("The user chose to abort the installation");

            // Continue with installation
            context.SetPercent(Size.From(50), Size.From(100));
            // await Task.Delay(1000, context.CancellationToken); // Simulate installation work
            context.SetPercent(Size.From(100), Size.From(100));

            return true;
        }
        catch (OperationCanceledException ex)
        {
            // Propagate cancellation from external components
            context.CancelAndThrow(ex.Message);
            return false; // Never reached, but required for compilation
        }
    }

    private static Task<bool> SimulateUserInteraction()
    {
        // Simulate user choosing to cancel (80% chance to continue for demo)
        return Task.FromResult(Random.Shared.Next(0, 10) < 8);
    }
}
