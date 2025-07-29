using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
// ReSharper disable LocalizableElement
namespace NexusMods.Jobs.Tests.Examples.BestPractices;

// Use percentage for determinate progress when total work is known.
// Report both progress percentage and rate when applicable using SetRateOfProgress().

[PublicAPI]
public class DeterminateProgressExample(IJobMonitor jobMonitor, TemporaryFileManager temporaryFileManager)
{
    [Fact]
    public async Task DemonstrateProgressReporting()
    {
        await using var tempFile = temporaryFileManager.CreateFile();
        var job = new ProgressTrackingDownloadJob
        {
            Uri = new Uri("https://example.com/largefile.zip"),
            Destination = tempFile.Path,
        };

        var jobTask = jobMonitor.Begin<ProgressTrackingDownloadJob, AbsolutePath>(job);

        // You can observe progress through the job system
        // The job will report progress via SetPercent and SetRateOfProgress
        var result = await jobTask;
        result.Should().Be(job.Destination);
    }
}

public record ProgressTrackingDownloadJob : IJobDefinitionWithStart<ProgressTrackingDownloadJob, AbsolutePath>
{
    public required Uri Uri { get; init; }
    public required AbsolutePath Destination { get; init; }

    public async ValueTask<AbsolutePath> StartAsync(IJobContext<ProgressTrackingDownloadJob> context)
    {
        var definition = context.Definition;

        // Simulate downloading with progress tracking
        var totalBytes = Size.From(1024 * 1024 * 16); // 16MB default (pretend we got this from content header)
        var downloadedBytes = Size.Zero;
        var chunkSize = Size.From(64 * 1024); // 64KB chunks

        context.SetPercent(downloadedBytes, totalBytes);

        // Simulate chunked download with progress reporting
        while (downloadedBytes < totalBytes)
        {
            // Simulate download chunk
            // await Task.Delay(8, context.CancellationToken); // Simulate network delay
            
            var bytesToDownload = chunkSize.Value <= (totalBytes - downloadedBytes).Value 
                ? chunkSize 
                : totalBytes - downloadedBytes;
            downloadedBytes += bytesToDownload;
            
            // Update percentage progress
            context.SetPercent(downloadedBytes, totalBytes);
            
            // Calculate and report download speed (simulated)
            var speedBytesPerSecond = chunkSize.Value * 10; // Simulate 640 KB/s
            context.SetRateOfProgress(speedBytesPerSecond);
            
            await context.YieldAsync();
        }

        return definition.Destination;
    }
}
