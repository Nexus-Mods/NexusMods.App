using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
// ReSharper disable LocalizableElement

namespace NexusMods.Jobs.Tests.Examples.BestPractices;

// Jobs can be immediately interrupted during nested async operations by enabling force pause.
// This is useful for jobs that need to be paused quickly, even during deep processing.
// Or when you need to call long-lived external code which only supports a `CancellationToken`.

[PublicAPI]
public class ForcePauseExample(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task DemonstrateForcePause()
    {
        // Simple demonstration of how to implement force pause capability
        const int fileCount = 3;
        var job = new ForcePausableProcessingJob(fileCount);
        var jobTask = jobMonitor.Begin<ForcePausableProcessingJob, int>(job);

        // Let the job complete.
        var result = await jobTask;
        result.Should().Be(fileCount);
    }
}

// Force pause job - can be interrupted immediately during nested async operations
public record ForcePausableProcessingJob(int FileCount) : IJobDefinitionWithStart<ForcePausableProcessingJob, int>
{
    // Opt into 'force pause' support for this job.
    // This requires you handle it with HandlePauseExceptionAsync below.
    public bool SupportsForcePause => true; 

    public async ValueTask<int> StartAsync(IJobContext<ForcePausableProcessingJob> context)
    {
        var processedFiles = 0;
        
        while (true)
        {
            try
            {
                return await ProcessFiles(context, processedFiles);
            }
            catch (OperationCanceledException ex)
            {
                // Handle force pause
                // If this was a cancellation, the exception will be rethrown here.
                // Otherwise, the code will continue until the job is resumed.
                await context.HandlePauseExceptionAsync(ex);
                
                // Continue loop to resume from where we left off
                // Note: `context.CancellationToken` has a new token
            }
        }
    }
    
    private async ValueTask<int> ProcessFiles(IJobContext<ForcePausableProcessingJob> context, int startFrom)
    {
        for (var x = startFrom; x < FileCount; x++)
        {
            // Check for pause/cancellation requests
            await context.YieldAsync(); 

            // Deep processing that can be interrupted immediately
            await ProcessSingleFile(x, context.CancellationToken);
            context.SetPercent(Size.FromLong(x + 1), Size.FromLong(FileCount));
        }
        
        return FileCount; // Return number of files processed
    }
    
    private async Task ProcessSingleFile(int fileIndex, CancellationToken cancellationToken)
    {
        // Simulate deep nested work - all operations respect cancellation token
        await Task.Delay(100, cancellationToken); // Simulate File I/O
        await AnalyzeFile(fileIndex, cancellationToken); // Simulate Analysis
    }
    
    private async Task AnalyzeFile(int fileIndex, CancellationToken cancellationToken)
    {
        // Even deeper nesting - can be interrupted immediately
        await Task.Delay(50, cancellationToken);
        _ = fileIndex;
    }
}

