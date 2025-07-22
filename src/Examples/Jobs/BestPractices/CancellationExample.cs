using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
// ReSharper disable LocalizableElement

namespace Examples.Jobs.BestPractices;

[PublicAPI]
public static class CancellationExample
{
    public static async Task DemonstrateStandardCancellation(IServiceProvider serviceProvider)
    {
        var jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        
        var job = new CancellableDownloadJob
        {
            DownloadUrl = new Uri("https://example.com/file.zip"),
            Destination = default(AbsolutePath),
        };
        
        var jobTask = jobMonitor.Begin<CancellableDownloadJob, AbsolutePath>(job);
        
        // For job callers - cancel jobs using:
        //jobMonitor.Cancel(jobTask);
        //jobMonitor.CancelGroup(jobGroup);
        // jobMonitor.CancelAll();
        // TODO: I will fix this in upcoming PR. This has been broken, due to missing abstractions. - Sewer
        
        _ = await jobTask;
    }
}

public record CancellableDownloadJob : IJobDefinitionWithStart<CancellableDownloadJob, AbsolutePath>
{
    public required Uri DownloadUrl { get; init; }
    public required AbsolutePath Destination { get; init; }
    
    public async ValueTask<AbsolutePath> StartAsync(IJobContext<CancellableDownloadJob> context)
    {
        // Check for cancellation before starting
        await context.YieldAsync();

        // Any resources that need clean-up should be managed with 'using' statements
        // to ensure they are disposed if cancellation or exception (e.g. via context.YieldAsync()) occurs.
        using var tempFileManager = new TemporaryFileManager(FileSystem.Shared); // 👈 get this from DI instead.
        await using var tempFile = tempFileManager.CreateFile();

        // Simulate download with periodic cancellation checks
        for (var x = 0; x <= 100; x += 10)
        {
            // Call YieldAsync() around expensive operations
            // and for periodic cancellation checks
            await context.YieldAsync();
            context.SetPercent(Size.From((ulong)x), Size.From(100));
            await Task.Delay(200, context.CancellationToken);
        }

        // Handle resource cleanup within job execution logic before completion.
        // Job system doesn't dispose IDisposable jobs automatically.
        // The 'using' statements above handle cleanup automatically.

        return context.Definition.Destination;
    }
}
