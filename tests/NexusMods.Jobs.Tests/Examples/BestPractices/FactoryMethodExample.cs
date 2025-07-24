using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
namespace NexusMods.Jobs.Tests.Examples.BestPractices;

// Factory methods like 'create' are useful when you want to fire a job right away after it is created.
// They encapsulate job creation and job-starting logic as one operation.

[PublicAPI]
public class FactoryMethodExample(IServiceProvider serviceProvider, TemporaryFileManager temporaryFileManager)
{
    [Fact]
    public async Task TestFactoryMethod()
    {
        await using var tempFile = temporaryFileManager.CreateFile();

        var uri = new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=RDdQw4w9WgXcQ");
        var destination = tempFile.Path;

        var jobTask = HttpDownloadJob.Create(serviceProvider, uri, destination);
        var result = await jobTask;

        result.Should().Be(destination);
    }
}

// HttpDownloadJob.cs
public record HttpDownloadJob : IJobDefinitionWithStart<HttpDownloadJob, AbsolutePath>
{
    public required Uri Uri { get; init; }
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
        AbsolutePath destination)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new HttpDownloadJob
        {
            Uri = uri,
            Destination = destination,
            Logger = provider.GetRequiredService<ILogger<HttpDownloadJob>>(),
            Client = provider.GetRequiredService<HttpClient>(),
        };
        return monitor.Begin<HttpDownloadJob, AbsolutePath>(job);
    }
}
