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
        await Assert.That(progressUpdates).IsNotEmpty();
        await Assert.That(progressUpdates.Count).IsGreaterThanOrEqualTo(4);

        // Progress should be monotonically increasing
        for (var x = 1; x < progressUpdates.Count; x++)
            await Assert.That(progressUpdates[x]).IsGreaterThanOrEqualTo(progressUpdates[x - 1]);

        // Final progress should be 100%
        await Assert.That(Math.Abs(progressUpdates.Last() - 1.0)).IsLessThanOrEqualTo(0.01);
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
        await Assert.That(rateUpdates).IsNotEmpty();
        await Assert.That(rateUpdates.All(r => Math.Abs(r - 20.0) < 0.00001)).IsTrue();
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
        await Assert.That(progressUpdates).IsNotEmpty();

        // Initial progress should be 0
        await Assert.That(progressUpdates.First()).IsEqualTo(0.0);

        // Final progress should be 1 (100%)
        await Assert.That(progressUpdates.Last()).IsEqualTo(1.0);

        // Rate of progress should be reported
        await Assert.That(rateUpdates).IsNotEmpty();
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        await Assert.That(rateUpdates.All(r => r == job.ExpectedRate)).IsTrue();

        // Result should match processed item count
        var result = await task;
        await Assert.That(result).IsEqualTo(job.ItemCount);
    }
}
