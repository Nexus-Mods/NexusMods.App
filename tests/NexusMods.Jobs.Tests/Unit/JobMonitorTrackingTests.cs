using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Jobs.Tests.TestInfrastructure;
using Xunit;
using DynamicData;

namespace NexusMods.Jobs.Tests.Unit;

public class JobMonitorTrackingTests(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task Should_Filter_ChangeSet_By_Job_Type()
    {
        // Arrange
        var progressJob = new ProgressReportingJob(StepCount: 2, StepDelay: TimeSpan.FromMilliseconds(10));
        var simpleJob = new SimpleTestJob();
        
        var progressJobChanges = new List<IChangeSet<IJob, JobId>>();
        var simpleJobChanges = new List<IChangeSet<IJob, JobId>>();
        
        // Act
        using var progressSubscription = jobMonitor.GetObservableChangeSet<ProgressReportingJob>()
            .Subscribe(changeSet => progressJobChanges.Add(changeSet));
        
        using var simpleSubscription = jobMonitor.GetObservableChangeSet<SimpleTestJob>()
            .Subscribe(changeSet => simpleJobChanges.Add(changeSet));
        
        var progressTask = jobMonitor.Begin<ProgressReportingJob, double>(progressJob);
        var simpleTask = jobMonitor.Begin<SimpleTestJob, string>(simpleJob);
        
        await Task.WhenAll(progressTask.Job.WaitAsync(), simpleTask.Job.WaitAsync());
        
        // Allow some time for change notifications
        await Task.Delay(50);
        
        // Assert
        progressJobChanges.Should().NotBeEmpty();
        simpleJobChanges.Should().NotBeEmpty();
        
        // Verify that each filtered stream only contains jobs of the correct type
        var progressJobs = progressJobChanges.SelectMany(cs => cs).Select(change => change.Current);
        var simpleJobs = simpleJobChanges.SelectMany(cs => cs).Select(change => change.Current);
        
        progressJobs.Should().OnlyContain(job => job.Definition is ProgressReportingJob);
        simpleJobs.Should().OnlyContain(job => job.Definition is SimpleTestJob);
    }

    [Fact]
    public async Task Should_Emit_ChangeSet_Events_For_Job_Lifecycle()
    {
        // Arrange
        var job = new SimpleTestJob();
        var changeSets = new List<IChangeSet<IJob, JobId>>();
        var changeSignal = new AutoResetEvent(false);
        
        // Act
        using var subscription = jobMonitor.GetObservableChangeSet<SimpleTestJob>()
            .Subscribe(changeSet => 
            {
                changeSets.Add(changeSet);
                changeSignal.Set();
            });
        
        var task = jobMonitor.Begin<SimpleTestJob, string>(job);
        
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

    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
