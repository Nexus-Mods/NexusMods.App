using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Jobs.Tests.TestInfrastructure;
using NexusMods.Paths;
using Xunit;
// ReSharper disable LocalizableElement

namespace NexusMods.Jobs.Tests.Examples.BestPractices;

// Jobs can be paused and resumed cooperatively. Jobs must call YieldAsync() 
// to respect pause requests.

[PublicAPI]
public class PauseResumeExample(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task DemonstratePauseAndResume()
    {
        const int iterationCount = 10;
        var job = new SimplePausableJob(iterationCount);
        var jobTask = jobMonitor.Begin<SimplePausableJob, int>(job);

        // Pause the job
        jobMonitor.Pause(jobTask);

        // You can also cancel while paused - cancellation takes precedence.
        // jobMonitor.Cancel(jobTask);
        
        // Wait for it to reach paused state
        await SynchronizationHelpers.WaitForJobState(jobTask.Job, JobStatus.Paused, TimeSpan.FromSeconds(5));
        jobTask.Job.Status.Should().Be(JobStatus.Paused);
        
        // Once paused, you can resume the job.
        // No 'magic' is required here.
        // It will pick up after the last YieldAsync() call.
        jobMonitor.Resume(jobTask);
        
        // Wait for completion
        var result = await jobTask;
        result.Should().Be(iterationCount);
    }
}

public record SimplePausableJob(int IterationCount) : IJobDefinitionWithStart<SimplePausableJob, int>
{
    public async ValueTask<int> StartAsync(IJobContext<SimplePausableJob> context)
    {
        var completed = 0;
        for (var x = 0; x < IterationCount; x++)
        {
            // Call YieldAsync() to allow pause/resume (and cancellation) at this point
            await context.YieldAsync();
            
            // Simulate some work
            await Task.Delay(TimeSpan.FromMilliseconds(30), context.CancellationToken);
            completed++;
            
            // Report progress
            context.SetPercent(Size.FromLong(completed), Size.FromLong(IterationCount));
        }

        return completed;
    }
}
