using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Jobs.Tests.TestInfrastructure;
using Xunit;

namespace NexusMods.Jobs.Tests.Unit;

public class JobStateTransitionTests(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task Should_Start_And_Complete_Successfully()
    {
        // Arrange
        var startSignal = new ManualResetEventSlim();
        var job = new SignaledJob(startSignal);
        
        // Act
        var task = jobMonitor.Begin<SignaledJob, bool>(job);
        
        // Assert
        task.Job.Status.Should().BeOneOf(JobStatus.None, JobStatus.Created, JobStatus.Running);
        startSignal.Set(); // Allow job to complete
        var result = await task;
        
        // Verify successful completion
        result.Should().BeTrue();
        task.Job.Status.Should().Be(JobStatus.Completed);
    }

    [Fact]
    public async Task Should_Transition_From_Running_To_Completed()
    {
        // Arrange
        var startSignal = new ManualResetEventSlim();
        var job = new SignaledJob(startSignal);
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = jobMonitor.Begin<SignaledJob, bool>(job);
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        
        // Wait for job to reach `Running` state before signaling
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Running, TimeSpan.FromSeconds(30));
        
        // Now signal completion, to let the job finish and transition to `Completed`
        startSignal.Set();
        var result = await task;
        
        // Assert
        result.Should().BeTrue();
        task.Job.Status.Should().Be(JobStatus.Completed);
        stateChanges.Should().Contain(JobStatus.Completed);
        // Note(sewer): We have no control up until the job starts with `Running`,
        // so we can't verify Created/None state.
    }

    [Fact]
    public async Task Should_Transition_From_Running_To_Failed_On_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var failSignal = new ManualResetEventSlim();
        var job = new FailingJob(expectedException, failSignal);
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = jobMonitor.Begin<FailingJob, string>(job);
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        
        // Wait for job to reach `Running` state before signaling failure
        await SynchronizationHelpers.WaitForJobState(task.Job, JobStatus.Running, TimeSpan.FromSeconds(30));
        
        // Now signal the job to fail
        failSignal.Set();
        
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => task.Job.WaitAsync());
        
        task.Job.Status.Should().Be(JobStatus.Failed);
        stateChanges.Should().ContainInOrder(JobStatus.Running, JobStatus.Failed);
    }

    [Fact]
    public async Task Should_Transition_From_Running_To_Cancelled()
    {
        // Arrange
        var readySignal = new ManualResetEventSlim();
        var job = new WaitForCancellationJob(readySignal);
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = jobMonitor.Begin<WaitForCancellationJob, string>(job);
        
        // Wait for job to be ready, then cancel
        readySignal.Wait(TimeSpan.FromSeconds(30));
        
        // Note(sewer): The job is running by definition, since readySignal was signaled on job's end.
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        jobMonitor.Cancel(task.Job.Id);
        
        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.Job.WaitAsync());
        
        task.Job.Status.Should().Be(JobStatus.Cancelled);
        stateChanges.Should().ContainInOrder(JobStatus.Cancelled);
    }

    // Note(sewer): Should_Self_Cancel_Via_CancelAndThrow is tested in JobCancellationTests.cs

    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
