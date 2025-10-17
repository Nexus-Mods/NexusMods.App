using NexusMods.Backend.Tests.Jobs.TestInfrastructure;
using NexusMods.Jobs.Tests;
using NexusMods.Jobs.Tests.TestInfrastructure;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Backend.Tests.Jobs.Unit;

public class JobStateTransitionTests : AJobsTest
{
    [Test]
    public async Task Should_Start_And_Complete_Successfully()
    {
        // Arrange
        var startSignal = new ManualResetEventSlim();
        var job = new SignaledJob(startSignal);

        // Act
        var task = JobMonitor.Begin<SignaledJob, bool>(job);

        // Assert
        await Assert.That(task.Job.Status == JobStatus.None || task.Job.Status == JobStatus.Created || task.Job.Status == JobStatus.Running).IsTrue();
        startSignal.Set(); // Allow job to complete
        var result = await task;

        // Verify successful completion
        await Assert.That(result).IsTrue();
        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Completed);
    }

    [Test]
    public async Task Should_Transition_From_Running_To_Completed()
    {
        // Arrange
        var startSignal = new ManualResetEventSlim();
        var job = new SignaledJob(startSignal);
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = JobMonitor.Begin<SignaledJob, bool>(job);
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        
        // Wait for job to reach `Running` state before signaling
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Running, TimeSpan.FromSeconds(30));
        
        // Now signal completion, to let the job finish and transition to `Completed`
        startSignal.Set();
        var result = await task;
        
        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Completed);
        await Assert.That(stateChanges).Contains(JobStatus.Completed);
        // Note(sewer): We have no control up until the job starts with `Running`,
        // so we can't verify Created/None state.
    }

    [Test]
    public async Task Should_Transition_From_Running_To_Failed_On_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var failSignal = new ManualResetEventSlim();
        var job = new FailingJob(expectedException, failSignal);
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = JobMonitor.Begin<FailingJob, string>(job);
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        
        // Wait for job to reach `Running` state before signaling failure
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Running, TimeSpan.FromSeconds(30));
        
        // Now signal the job to fail
        failSignal.Set();
        
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => task.Job.WaitAsync());

        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Failed);
        await Assert.That(stateChanges).IsEquivalentTo([JobStatus.Created, JobStatus.Running, JobStatus.Failed]);
    }

    [Test]
    public async Task Should_Transition_From_Running_To_Cancelled()
    {
        // Arrange
        var readySignal = new ManualResetEventSlim();
        var job = new WaitForCancellationJob(readySignal);
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = JobMonitor.Begin<WaitForCancellationJob, string>(job);
        
        // Wait for job to be ready, then cancel
        readySignal.Wait(TimeSpan.FromSeconds(30));
        
        // Note(sewer): The job is running by definition, since readySignal was signaled on job's end.
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        JobMonitor.Cancel(task.Job.Id);
        
        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => task.Job.WaitAsync());

        await Assert.That(task.Job.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(stateChanges).IsEquivalentTo([JobStatus.Running, JobStatus.Cancelled]);
    }
}
