using NexusMods.Jobs.Tests;
using NexusMods.Jobs.Tests.TestInfrastructure;
using System.Reactive.Linq;

namespace NexusMods.Backend.Tests.Jobs.Unit;

public class JobProgressReportingTests : AJobsTest
{
    [Test]
    public async Task Should_Report_Progress_As_Percentage()
    {
        // Arrange
        var startSignal = new ManualResetEventSlim();
        var job = new ProgressReportingJob(StepCount: 4, StepDelay: TimeSpan.FromMilliseconds(10), startSignal);
        var progressUpdates = new List<double>();
        
        // Act
        var task = JobMonitor.Begin<ProgressReportingJob, double>(job);
        
        using var subscription = task.Job.ObservableProgress
            .Where(p => p.HasValue)
            .Subscribe(progress => progressUpdates.Add(progress.Value.Value));
        
        // Signal job to start after subscription is set up
        startSignal.Set();
        
        // Wait for completion
        await task;
        
        // Assert
        progressUpdates.Should().NotBeEmpty();
        progressUpdates.Should().HaveCountGreaterOrEqualTo(4);
        
        // Progress should be monotonically increasing
        for (var x = 1; x < progressUpdates.Count; x++)
            progressUpdates[x].Should().BeGreaterOrEqualTo(progressUpdates[x - 1]);
        
        // Final progress should be 100%
        progressUpdates.Last().Should().BeApproximately(1.0, 0.01);
    }

    [Test]
    public async Task Should_Report_Rate_Of_Progress()
    {
        // Arrange
        var startSignal = new ManualResetEventSlim();
        var job = new ProgressReportingJob(StepCount: 3, StepDelay: TimeSpan.FromMilliseconds(50), startSignal);
        var rateUpdates = new List<double>();
        
        // Act
        var task = JobMonitor.Begin<ProgressReportingJob, double>(job);
        
        using var subscription = task.Job.ObservableRateOfProgress
            .Where(r => r.HasValue)
            .Subscribe(rate => rateUpdates.Add(rate.Value));
        
        // Signal job to start after subscription is set up
        startSignal.Set();
        
        await task;
        
        // Assert
        rateUpdates.Should().NotBeEmpty();
        rateUpdates.Should().OnlyContain(r => Math.Abs(r - 20.0) < 0.00001);
    }

    [Test]
    public async Task Should_Handle_Indeterminate_Progress()
    {
        // Test jobs that report indeterminate progress (unknown total work)

        // Arrange
        var startSignal = new ManualResetEventSlim();
        var job = new IndeterminateProgressJob(ItemCount: 5, ItemDelay: TimeSpan.FromMilliseconds(20), startSignal);
        var progressUpdates = new List<double>();
        var rateUpdates = new List<double>();
        
        // Act
        var task = JobMonitor.Begin<IndeterminateProgressJob, int>(job);
        
        using var progressSub = task.Job.ObservableProgress
            .Where(p => p.HasValue)
            .Subscribe(progress => progressUpdates.Add(progress.Value.Value));
            
        using var rateSub = task.Job.ObservableRateOfProgress
            .Where(r => r.HasValue)
            .Subscribe(rate => rateUpdates.Add(rate.Value));

        // Signal job to start after subscriptions are set up
        startSignal.Set();
        
        await task;
        
        // Assert
        // Should have progress updates
        progressUpdates.Should().NotBeEmpty();
        
        // Initial progress should be 0
        progressUpdates.First().Should().Be(0.0);
        
        // Final progress should be 1 (100%)
        progressUpdates.Last().Should().Be(1.0);
        
        // Rate of progress should be reported
        rateUpdates.Should().NotBeEmpty();
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        rateUpdates.Should().OnlyContain(r => r == job.ExpectedRate);
        
        // Result should match processed item count
        var result = await task;
        result.Should().Be(job.ItemCount);
    }
}
