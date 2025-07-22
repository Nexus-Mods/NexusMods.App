using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
// ReSharper disable LocalizableElement

namespace Examples.Jobs.BestPractices;

[PublicAPI]
public static class InteractiveCancellationExample
{
    public static async Task DemonstrateSelfCancellation(IServiceProvider serviceProvider)
    {
        var jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();

        var job = new InteractiveInstallJob { ItemName = "TestMod" };
        var jobTask = jobMonitor.Begin<InteractiveInstallJob, bool>(job);
        
        try
        {
            var result = await jobTask;
            Console.WriteLine($"Installation completed: {result}");
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine($"Installation cancelled: {ex.Message}");
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
            var shouldInstall = await SimulateUserInteraction(context.CancellationToken);
            
            if (!shouldInstall)
            {
                // Self-cancellation: The job cancels itself based on user input
                context.CancelAndThrow("The user chose to abort the installation");
            }
            
            // Continue with installation
            context.SetPercent(Size.From(50), Size.From(100));
            await Task.Delay(1000, context.CancellationToken);
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

    private static async Task<bool> SimulateUserInteraction(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        // Simulate user choosing to cancel (80% chance to continue for demo)
        return Random.Shared.Next(0, 10) < 8;
    }
}
