using JetBrains.Annotations;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;
using Xunit;
// ReSharper disable LocalizableElement

namespace NexusMods.Jobs.Tests.Examples.BestPractices;

[PublicAPI]
public class CancellationExample(IJobMonitor jobMonitor, TemporaryFileManager temporaryFileManager)
{
    [Fact]
    public async Task DemonstrateStandardCancellation()
    {
        await using var mockArchive = temporaryFileManager.CreateFile();
        var job = new ArchiveAnalysisJob
        {
            ArchivePath = mockArchive.Path,
        };

        var jobTask = jobMonitor.Begin<ArchiveAnalysisJob, AnalysisResults>(job);

        // Cancel the job right away!
        jobMonitor.Cancel(jobTask);

        // Because our job was canceled, it will throw a TaskCanceledException
        // await Assert.ThrowsAsync<TaskCanceledException>(async () => await jobTask);
        // unless it finished before cancellation was requested.
    }

    [Fact]
    public async Task DemonstrateSuccessfulCompletion()
    {
        await using var mockArchive = temporaryFileManager.CreateFile();
        var job = new ArchiveAnalysisJob
        {
            ArchivePath = mockArchive.Path,
        };
        
        var jobTask = jobMonitor.Begin<ArchiveAnalysisJob, AnalysisResults>(job);
        
        // Let the job complete without cancellation
        var result = await jobTask;
        result.Should().NotBeNull();
        result.ArchivePath.Should().Be(job.ArchivePath);
        result.FileCount.Should().Be(3);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DemonstrateJobStatusAndProgress()
    {
        await using var mockArchive = temporaryFileManager.CreateFile();
        var job = new ArchiveAnalysisJob
        {
            ArchivePath = mockArchive.Path,
        };

        var jobTask = jobMonitor.Begin<ArchiveAnalysisJob, AnalysisResults>(job);
        await jobTask; // Complete the job
        
        // Check final progress after completion
        var finalStatus = jobTask.Job.Status;
        var finalProgress = jobTask.Job.Progress.Value;
        
        finalStatus.Should().Be(JobStatus.Completed);
        finalProgress.Should().Be(Percent.CreateClamped(1)); // Should be 100% (done)
    }
}

// Pretend we're analyzing an archive file. Temporarily extracting some stuff, then
// discarding the extracted files.
public record ArchiveAnalysisJob : IJobDefinitionWithStart<ArchiveAnalysisJob, AnalysisResults>
{
    public const int FileCount = 3;
    public required AbsolutePath ArchivePath { get; init; }
    
    public async ValueTask<AnalysisResults> StartAsync(IJobContext<ArchiveAnalysisJob> context)
    {
        // Check for cancellation before starting
        await context.YieldAsync();

        // Any resources that need clean-up should be managed with 'using' statements
        // to ensure they are disposed if cancellation or exception (e.g. via context.YieldAsync()) occurs.
        // In this case, that's our extracted data.
        using var tempFileManager = new TemporaryFileManager(FileSystem.Shared); // 👈 get this from DI instead.
        await using var extractionFolder = tempFileManager.CreateFolder();

        // Simulate archive extraction and analysis with periodic cancellation checks
        var fileCount = FileCount; // Simulate 3 files in archive: manifest.json, data.bin, config.xml
        
        // Simulate archive extraction and analysis work
        for (var x = 0; x < FileCount; x++)
        {
            // Call YieldAsync() around expensive operations
            // and for periodic cancellation checks
            await context.YieldAsync();
            
            // Pretend we're extracting file from archive to temp folder
            // await Task.Delay(8);
            await context.YieldAsync();
            
            // Pretend we're analyzing the extracted file
            // await Task.Delay(4, context.CancellationToken);
            
            context.SetPercent(Size.FromLong(x), Size.FromLong(fileCount));
        }

        context.SetPercent(Size.FromLong(fileCount), Size.FromLong(fileCount));

        // Analysis is complete.
        return new AnalysisResults
        {
            ArchivePath = context.Definition.ArchivePath,
            FileCount = fileCount,
            IsValid = true,
        };
    }
}

public record AnalysisResults // Some arbitrary data.
{
    public required AbsolutePath ArchivePath { get; init; }
    public required int FileCount { get; init; }
    public required bool IsValid { get; init; }
}
