using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using NexusMods.Jobs.Tests.TestInfrastructure;
using Xunit;

namespace NexusMods.Jobs.Tests.Unit;

public class JobPauseResumeTests(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task Should_Pause_And_Resume_Job()
    {
        // Arrange
        var allowJobToPauseSignal = new ManualResetEventSlim();
        var jobIsPausingSignal = new ManualResetEventSlim();
        var allowJobToCompleteSignal = new ManualResetEventSlim();
        var jobHasResumedSignal = new ManualResetEventSlim();
        
        var job = new PauseResumeTestJob(allowJobToPauseSignal, jobIsPausingSignal, allowJobToCompleteSignal, jobHasResumedSignal);
        
        var task = jobMonitor.Begin<PauseResumeTestJob, int>(job);
        
        // Act
        // Mark the job as one to be paused.
        jobMonitor.Pause(task);
        
        // Allow the `PauseResumeTestJob` job to pause.
        allowJobToPauseSignal.Set();
        
        // Wait for job to reach paused state
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Paused, TimeSpan.FromSeconds(30));
        
        // Resume the job
        jobMonitor.Resume(task);
        
        // Wait for the job to signal it has resumed execution
        jobHasResumedSignal.Wait(TimeSpan.FromSeconds(30)).Should().BeTrue();
        task.Job.Status.Should().Be(JobStatus.Running);
        
        // Allow the job to complete
        allowJobToCompleteSignal.Set();
        
        // Wait for completion
        var result = await task;
        
        result.Should().Be(42);
        task.Job.Status.Should().Be(JobStatus.Completed);
    }

    [Fact]
    public async Task Should_Cancel_Paused_Job()
    {
        // Arrange
        var allowYieldSignal = new ManualResetEventSlim();
        
        var job = new SignaledJob(allowYieldSignal);
        var task = jobMonitor.Begin<SignaledJob, bool>(job);
        
        // Act
        jobMonitor.Pause(task);
        allowYieldSignal.Set(); // Allow job to hit yield, thus pause.
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Paused, TimeSpan.FromSeconds(30));
        
        // Cancel while paused
        jobMonitor.Cancel(task);
        
        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.Job.WaitAsync());
        task.Job.Status.Should().Be(JobStatus.Cancelled);
    }
}
