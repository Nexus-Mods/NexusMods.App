using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using NexusMods.Jobs.Tests.TestInfrastructure;
using Xunit;

namespace NexusMods.Jobs.Tests.Unit;

public class JobForcePauseResumeTests(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task Should_Handle_Multiple_Pause_Resume_Cycles()
    {
        // Arrange - Setup signals for 3 pause/resume cycles
        const int cycleCount = 3;
        var allowYieldSignals = Enumerable.Range(0, cycleCount)
            .Select(_ => new ManualResetEventSlim()).ToArray();
        var pausingSignals = Enumerable.Range(0, cycleCount)
            .Select(_ => new ManualResetEventSlim()).ToArray();
        var resumedSignals = Enumerable.Range(0, cycleCount)
            .Select(_ => new ManualResetEventSlim()).ToArray();
        
        var job = new MultiplePauseResumeTestJob(allowYieldSignals, pausingSignals, resumedSignals);
        
        var task = jobMonitor.Begin<MultiplePauseResumeTestJob, int>(job);
        
        try
        {
            // Act & Assert - Test multiple pause/resume cycles
            for (var cycle = 0; cycle < cycleCount; cycle++)
            {
                // 1. Mark job for pause BEFORE allowing it to proceed
                jobMonitor.Pause(task);
                
                // 2. Allow the job to proceed to pause point
                allowYieldSignals[cycle].Set();
                
                // 3. Wait for job to signal it's ready to pause (CI-safety, in case of long CI stalls)
                pausingSignals[cycle].Wait(TimeSpan.FromSeconds(60))
                    .Should().BeTrue($"Cycle {cycle} should signal pausing within timeout");
                
                // 4. Wait for job to reach paused state
                await SynchronizationHelpers.WaitForJobState(
                    task.Job, JobStatus.Paused, TimeSpan.FromSeconds(60));
                
                // 5. Resume the job
                jobMonitor.Resume(task);
                
                // 6. Wait for job to signal it has resumed
                resumedSignals[cycle].Wait(TimeSpan.FromSeconds(60))
                    .Should().BeTrue($"Cycle {cycle} should signal resume within timeout");
                
                // 7. Verify running state
                if (cycle < cycleCount - 1)
                    task.Job.Status.Should().Be(JobStatus.Running, $"Job should be running after cycle {cycle}");
                else
                    // On the last iteration, the job may complete before this assert runs.
                    // That is expected.
                    task.Job.Status.Should().BeOneOf(JobStatus.Running, JobStatus.Completed);
            }
            
            // Wait for completion
            var result = await task;
            
            // Assert final state - successful completion proves token recycling works
            result.Should().Be(cycleCount);
            task.Job.Status.Should().Be(JobStatus.Completed);
        }
        finally
        {
            // Cleanup: Dispose all ManualResetEventSlim instances
            foreach (var signal in allowYieldSignals.Concat(pausingSignals).Concat(resumedSignals))
                signal.Dispose();
        }
    }

    [Fact]
    public async Task Should_Distinguish_Between_Pause_And_Cancellation()
    {
        // Arrange
        var readyToPause = new ManualResetEventSlim();
        var job = new PauseCancellationDistinctionJob(readyToPause);
        
        var task = jobMonitor.Begin<PauseCancellationDistinctionJob, string>(job);
        
        try
        {
            // Wait for job to be ready
            readyToPause.Wait(TimeSpan.FromSeconds(10))
                .Should().BeTrue("Job should signal ready within timeout");
            
            // Test pause (should not throw)
            jobMonitor.Pause(task);
            await SynchronizationHelpers.WaitForJobState(
                task.Job, JobStatus.Paused, TimeSpan.FromSeconds(10));
            
            jobMonitor.Resume(task);
            await SynchronizationHelpers.WaitForJobState(
                task.Job, JobStatus.Running, TimeSpan.FromSeconds(10));
            
            // Test cancellation (should cause job to end)
            jobMonitor.Cancel(task);
            
            // Wait for cancellation
            await SynchronizationHelpers.WaitForJobState(
                task.Job, JobStatus.Cancelled, TimeSpan.FromSeconds(10));
            
            // Job should have been cancelled, not completed
            task.Job.Status.Should().Be(JobStatus.Cancelled);
        }
        finally
        {
            readyToPause.Dispose();
        }
    }
}

// Test job implementations

/// <summary>
/// Custom job for testing multiple pause/resume cycles with proper CI-safe coordination
/// </summary>
public record MultiplePauseResumeTestJob(
    ManualResetEventSlim[] AllowYieldSignals,
    ManualResetEventSlim[] PausingSignals,
    ManualResetEventSlim[] ResumedSignals) : IJobDefinitionWithStart<MultiplePauseResumeTestJob, int>
{
    public bool SupportsForcePause => true;
    
    public async ValueTask<int> StartAsync(IJobContext<MultiplePauseResumeTestJob> context)
    {
        var completedCycles = 0;
        
        while (true)
        {
            try
            {
                return await ExecuteCycles(context, completedCycles);
            }
            catch (OperationCanceledException ex)
            {
                // Handle force pause
                await context.HandlePauseExceptionAsync(ex);
                
                // Continue loop to resume from where we left off
            }
        }
    }
    
    private async ValueTask<int> ExecuteCycles(IJobContext<MultiplePauseResumeTestJob> context, int startFrom)
    {
        for (var cycle = startFrom; cycle < PausingSignals.Length; cycle++)
        {
            // Wait for test to allow us to proceed to pause point
            AllowYieldSignals[cycle].Wait();
            
            // Signal that we're about to pause (for CI timing)
            PausingSignals[cycle].Set();
            
            // This will pause if job monitor has called Pause()
            await context.YieldAsync();
            
            // Signal that we've resumed from this cycle
            ResumedSignals[cycle].Set();
        }

        return PausingSignals.Length;
    }
}

/// <summary>
/// Job to test distinguishing between pause and cancellation
/// </summary>
public record PauseCancellationDistinctionJob(
    ManualResetEventSlim ReadySignal) : IJobDefinitionWithStart<PauseCancellationDistinctionJob, string>
{
    public bool SupportsForcePause => true;
    
    public async ValueTask<string> StartAsync(IJobContext<PauseCancellationDistinctionJob> context)
    {
        // Signal ready
        ReadySignal.Set();
        
        while (true)
        {
            try
            {
                // Long running operation that can be interrupted
                await context.YieldAsync();
                await Task.Delay(10, context.CancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                // This should distinguish between pause and cancellation
                await context.HandlePauseExceptionAsync(ex);
                
                // If we get here, it was a pause, continue the loop from where we left off
            }
        }
    }
}
