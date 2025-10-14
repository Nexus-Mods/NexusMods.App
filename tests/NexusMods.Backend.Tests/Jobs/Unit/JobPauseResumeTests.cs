using FluentAssertions;
using NexusMods.Jobs.Tests;
using NexusMods.Jobs.Tests.TestInfrastructure;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Backend.Tests.Jobs.Unit;

public class JobPauseResumeTests : AJobsTest
{
    [Test]
    public async Task Should_Pause_And_Resume_Job()
    {
        // Arrange
        var allowJobToPauseSignal = new ManualResetEventSlim();
        var jobIsPausingSignal = new ManualResetEventSlim();
        var allowJobToCompleteSignal = new ManualResetEventSlim();
        var jobHasResumedSignal = new ManualResetEventSlim();
        
        var job = new PauseResumeTestJob(allowJobToPauseSignal, jobIsPausingSignal, allowJobToCompleteSignal, jobHasResumedSignal);
        
        var task = JobMonitor.Begin<PauseResumeTestJob, int>(job);
        
        // Act
        // Mark the job as one to be paused.
        JobMonitor.Pause(task);
        
        // Allow the `PauseResumeTestJob` job to pause.
        allowJobToPauseSignal.Set();
        
        // Wait for job to reach paused state
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Paused, TimeSpan.FromSeconds(30));
        
        // Resume the job
        JobMonitor.Resume(task);
        
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

    [Test]
    public async Task Should_Cancel_Paused_Job()
    {
        // Arrange
        var allowYieldSignal = new ManualResetEventSlim();
        
        var job = new SignaledJob(allowYieldSignal);
        var task = JobMonitor.Begin<SignaledJob, bool>(job);
        
        // Act
        JobMonitor.Pause(task);
        allowYieldSignal.Set(); // Allow job to hit yield, thus pause.
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Paused, TimeSpan.FromSeconds(30));
        
        // Cancel while paused
        JobMonitor.Cancel(task);
        
        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task.Job.WaitAsync());
        task.Job.Status.Should().Be(JobStatus.Cancelled);
    }

    [Test]
    public async Task Should_Cancel_Instead_Of_Pause_When_SupportsPausing_Is_False()
    {
        // Arrange
        var startSignal = new ManualResetEventSlim();
        var completionSignal = new ManualResetEventSlim();
        
        var job = new NonPausableJob(startSignal, completionSignal);
        var task = JobMonitor.Begin<NonPausableJob, string>(job);
        
        // Act
        startSignal.Set(); // Allow job to start and reach yield point
        
        // Wait for job to start running
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Running, TimeSpan.FromSeconds(5));
        
        // Try to pause - should cancel instead because SupportsPausing = false
        JobMonitor.Pause(task);
        
        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task.Job.WaitAsync());
        task.Job.Status.Should().Be(JobStatus.Cancelled);
    }
}
