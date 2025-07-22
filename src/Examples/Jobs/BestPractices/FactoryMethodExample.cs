using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace Examples.Jobs.BestPractices;

// Factory methods like 'create' are useful when you want to fire a job right away after it is created.
// They encapsulate job creation and job-starting logic as one operation.

// HttpDownloadJob.cs
public record HttpDownloadJob : IJobDefinitionWithStart<HttpDownloadJob, AbsolutePath>
{
    public required Uri Uri { get; init; }
    public required Uri DownloadPageUri { get; init; }
    public required AbsolutePath Destination { get; init; }
    public required ILogger<HttpDownloadJob> Logger { get; init; }
    public required HttpClient Client { get; init; }
    
    public async ValueTask<AbsolutePath> StartAsync(IJobContext<HttpDownloadJob> context)
    {
        // Stub implementation
        await Task.Delay(1000, context.CancellationToken);
        return context.Definition.Destination;
    }

    [PublicAPI]
    public static IJobTask<HttpDownloadJob, AbsolutePath> Create(
        IServiceProvider provider,
        Uri uri,
        Uri downloadPage,
        AbsolutePath destination)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new HttpDownloadJob
        {
            Uri = uri,
            DownloadPageUri = downloadPage,
            Destination = destination,
            Logger = provider.GetRequiredService<ILogger<HttpDownloadJob>>(),
            Client = provider.GetRequiredService<HttpClient>(),
        };
        return monitor.Begin<HttpDownloadJob, AbsolutePath>(job);
    }
}
