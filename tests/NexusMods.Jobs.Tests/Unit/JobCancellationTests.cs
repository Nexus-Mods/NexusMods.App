using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Jobs.Tests.TestInfrastructure;
using Xunit;

namespace NexusMods.Jobs.Tests.Unit;

public class JobCancellationTests(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task Should_Cancel_Job_By_Task_Reference()
    {
        // Arrange
        var readySignal = new ManualResetEventSlim();
        var job = new WaitForCancellationJob(readySignal);
        
        // Act
        var task = jobMonitor.Begin<WaitForCancellationJob, string>(job);
        
        // Wait for job to be ready
        readySignal.Wait(TimeSpan.FromSeconds(30));
        
        // Cancel by task reference
        jobMonitor.Cancel(task);
        
        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.Job.WaitAsync());
        task.Job.Status.Should().Be(JobStatus.Cancelled);
    }

    // TODO: Should_Cancel_All_Jobs_In_Group once there is a proper group API.
    //       Right now, there is not.

    [Fact]
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
        var tasks = jobs.Select(jobMonitor.Begin<WaitForCancellationJob, string>).ToArray();
        
        // Wait for all jobs to be ready
        // Note(sewer): Is there a world where a single core processor may not get enough threads on threadpool?
        foreach (var signal in readySignals) 
            signal.Wait(TimeSpan.FromSeconds(30));
        
        // Cancel all active jobs
        jobMonitor.CancelAll();
        
        // Assert
        foreach (var task in tasks)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.Job.WaitAsync());
            task.Job.Status.Should().Be(JobStatus.Cancelled);
        }
    }

    [Fact]
    public async Task Should_Self_Cancel_Via_CancelAndThrow()
    {
        // Arrange
        var job = new SelfCancellingJob();
        var stateChanges = new List<JobStatus>();
        
        // Act
        var task = jobMonitor.Begin<SelfCancellingJob, string>(job);
        using var subscription = task.Job.ObservableStatus.Subscribe(status => stateChanges.Add(status));
        
        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.Job.WaitAsync());
        
        task.Job.Status.Should().Be(JobStatus.Cancelled);
        stateChanges.Should().ContainInOrder(JobStatus.Running, JobStatus.Cancelled);
    }

    [Fact]
    public async Task Should_Not_Cancel_Completed_Jobs()
    {
        // Arrange
        var job = new SimpleTestJob();
        
        // Act
        var task = jobMonitor.Begin<SimpleTestJob, string>(job);
        
        // Wait for completion
        var result = await task;
        
        // Try to cancel after completion
        jobMonitor.Cancel(task.Job.Id);
        
        // Assert - Job should remain completed
        task.Job.Status.Should().Be(JobStatus.Completed);
        result.Should().Be("Completed");
    }

    [Fact]
    public async Task Should_Cancel_Job_That_Never_Yields()
    {
        // This test demonstrates that jobs which don't call YieldAsync() may not be cancellable
        // This is expected behavior based on the design
        
        // Arrange
        var startedSignal = new ManualResetEventSlim();
        var completionSignal = new ManualResetEventSlim();
        var job = new NonYieldingJob(completionSignal, startedSignal);
        
        // Act
        var task = jobMonitor.Begin<NonYieldingJob, string>(job);
        
        // Wait for job to start before trying to cancel
        startedSignal.Wait(TimeSpan.FromSeconds(30));
        
        // Allow the job to complete
        completionSignal.Set();
        
        // Cancel the job
        jobMonitor.Cancel(task.Job.Id);

        // Assert - Since the job never calls YieldAsync() and doesn't use cancellation-aware APIs,
        // it should complete normally despite the cancellation request
        var result = await task;
        result.Should().Be("Non-yielding work completed");
        task.Job.Status.Should().Be(JobStatus.Completed);
    }

    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
