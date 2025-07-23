using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
// ReSharper disable LocalizableElement

namespace Examples.Jobs.BestPractices;

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
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await jobTask);
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
        var finalStatus = jobTask.JobInstance.Status;
        var finalProgress = jobTask.JobInstance.Progress.Value;
        
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
            
            // Simulate extracting file from archive to temp folder
            await Task.Delay(1000, context.CancellationToken); // Simulate extraction time
            await context.YieldAsync();
            
            // Simulate analyzing the extracted file
            await Task.Delay(500, context.CancellationToken); // Simulate analysis time
            
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
