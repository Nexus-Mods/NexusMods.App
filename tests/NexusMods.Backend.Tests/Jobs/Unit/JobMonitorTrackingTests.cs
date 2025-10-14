using DynamicData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Jobs.Tests;
using NexusMods.Jobs.Tests.TestInfrastructure;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Backend.Tests.Jobs.Unit;

public class JobMonitorTrackingTests : AJobsTest
{
    [Test]
    public async Task Should_Filter_ChangeSet_By_Job_Type()
    {
        // Arrange
        var progressJob = new ProgressReportingJob(StepCount: 2, StepDelay: TimeSpan.FromMilliseconds(10));
        var simpleJob = new SimpleTestJob();
        
        var progressJobChanges = new List<IChangeSet<IJob, JobId>>();
        var simpleJobChanges = new List<IChangeSet<IJob, JobId>>();
        
        // Act
        using var progressSubscription = JobMonitor.GetObservableChangeSet<ProgressReportingJob>()
            .Subscribe(changeSet => progressJobChanges.Add(changeSet));
        
        using var simpleSubscription = JobMonitor.GetObservableChangeSet<SimpleTestJob>()
            .Subscribe(changeSet => simpleJobChanges.Add(changeSet));
        
        var progressTask = JobMonitor.Begin<ProgressReportingJob, double>(progressJob);
        var simpleTask = JobMonitor.Begin<SimpleTestJob, string>(simpleJob);
        
        await Task.WhenAll(progressTask.Job.WaitAsync(), simpleTask.Job.WaitAsync());
        
        // Assert
        progressJobChanges.Should().NotBeEmpty();
        simpleJobChanges.Should().NotBeEmpty();
        
        // Verify that each filtered stream only contains jobs of the correct type
        var progressJobs = progressJobChanges.SelectMany(cs => cs).Select(change => change.Current);
        var simpleJobs = simpleJobChanges.SelectMany(cs => cs).Select(change => change.Current);
        
        progressJobs.Should().OnlyContain(job => job.Definition is ProgressReportingJob);
        simpleJobs.Should().OnlyContain(job => job.Definition is SimpleTestJob);
    }

    [Test]
    public async Task Should_Emit_ChangeSet_Events_For_Job_Lifecycle()
    {
        // Arrange
        var job = new SimpleTestJob();
        var changeSets = new List<IChangeSet<IJob, JobId>>();
        var changeSignal = new AutoResetEvent(false);
        
        // Act
        using var subscription = JobMonitor.GetObservableChangeSet<SimpleTestJob>()
            .Subscribe(changeSet => 
            {
                changeSets.Add(changeSet);
                changeSignal.Set();
            });
        
        var task = JobMonitor.Begin<SimpleTestJob, string>(job);
        
        // Wait for initial change set
        changeSignal.WaitOne(TimeSpan.FromSeconds(2));
        
        await task;
        
        // Wait for potential removal change set
        changeSignal.WaitOne(TimeSpan.FromSeconds(2));
        
        // Assert
        changeSets.Should().NotBeEmpty();
        
        // Should have at least one Add operation
        var hasAddOperation = changeSets.Any(cs => cs.Any(change => change.Reason == ChangeReason.Add));
        hasAddOperation.Should().BeTrue();
    }
}
