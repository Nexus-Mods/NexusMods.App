using NexusMods.Jobs.Tests;
using NexusMods.Jobs.Tests.TestInfrastructure;
using NexusMods.Sdk.Jobs;
using TUnit.Assertions;

namespace NexusMods.Backend.Tests.Jobs.Unit;

public class JobCancellationTests : AJobsTest
{
    [Test]
    public async Task Should_Cancel_Job_By_Task_Reference()
    {
        // Arrange
        var readySignal = new ManualResetEventSlim();
        var job = new WaitForCancellationJob(readySignal);
        
        // Act
        var task = JobMonitor.Begin<WaitForCancellationJob, string>(job);
        
        // Wait for job to be ready
        readySignal.Wait(TimeSpan.FromSeconds(30));
        
        // Cancel by task reference
        JobMonitor.Cancel(task);
        
        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task.Job.WaitAsync());
        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Cancelled);
    }

    // TODO: Should_Cancel_All_Jobs_In_Group once there is a proper group API.
    //       Right now, there is not.

    [Test]
    public async Task Should_Cancel_All_Active_Jobs()
    {
        // Arrange
        var jobs = new[]
        {
            new WaitForCancellationJob(new ManualResetEventSlim()),
            new WaitForCancellationJob(new ManualResetEventSlim()),
            new WaitForCancellationJob(new ManualResetEventSlim()),
        };
        
        var readySignals = jobs.Select(job => job.ReadySignal).ToArray();
        
        // Act - Start multiple jobs
        var tasks = jobs.Select(JobMonitor.Begin<WaitForCancellationJob, string>).ToArray();
        
        // Wait for all jobs to be ready
        // Note(sewer): Is there a world where a single core processor may not get enough threads on threadpool?
        foreach (var signal in readySignals) 
            signal.Wait(TimeSpan.FromSeconds(30));
        
        // Cancel all active jobs
        JobMonitor.CancelAll();
        
        // Assert
        foreach (var task in tasks)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task.Job.WaitAsync());
            await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Cancelled);
        }
    }

    [Test]
    public async Task Should_Self_Cancel_Via_CancelAndThrow()
    {
        // Arrange
        var job = new SelfCancellingJob();
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = JobMonitor.Begin<SelfCancellingJob, string>(job);
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        
        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task.Job.WaitAsync());
        
        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(stateChanges).Contains(JobStatus.Cancelled);
    }

    [Test]
    public async Task Should_Not_Cancel_Completed_Jobs()
    {
        // Arrange
        var job = new SimpleTestJob();
        
        // Act
        var task = JobMonitor.Begin<SimpleTestJob, string>(job);
        
        // Wait for completion
        var result = await task;
        
        // Try to cancel after completion
        JobMonitor.Cancel(task.Job.Id);
        
        // Assert - Job should remain completed
        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Completed);
        await Assert.That(result).IsEqualTo("Completed");
    }

    [Test]
    public async Task Should_Cancel_Job_That_Never_Yields()
    {
        // This test demonstrates that jobs which don't call YieldAsync() may not be cancellable
        // This is expected behavior based on the design
        
        // Arrange
        var startedSignal = new ManualResetEventSlim();
        var completionSignal = new ManualResetEventSlim();
        var job = new NonYieldingJob(completionSignal, startedSignal);
        
        // Act
        var task = JobMonitor.Begin<NonYieldingJob, string>(job);
        
        // Wait for job to start before trying to cancel
        startedSignal.Wait(TimeSpan.FromSeconds(30));
        
        // Allow the job to complete
        completionSignal.Set();
        
        // Cancel the job
        JobMonitor.Cancel(task.Job.Id);

        // Assert - Since the job never calls YieldAsync() and doesn't use cancellation-aware APIs,
        // it should complete normally despite the cancellation request
        var result = await task;
        await Assert.That(result).IsEqualTo("Non-yielding work completed");
        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Completed);
    }
}
