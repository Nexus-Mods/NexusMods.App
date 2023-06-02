using FluentAssertions;
using Moq;
using NexusMods.Paths;

namespace NexusMods.DataModel.RateLimiting.Tests;

public class JobThroughputTests
{
    // Note: This new system is not super duper accurate (no floating point math for sizes), but it's good enough for our purposes
    //       given that we do the counting in bytes.
    [Theory]
    [InlineData(100, 1, 1f, 1)]
    [InlineData(100, 5, 1f, 5)]
    [InlineData(100, 50, 2f, 25)]
    [InlineData(100, 7, 2f, 3)]
    public async Task ReportsCorrectThroughput(int jobSize, int processed, float timeElapsed, int expectedThroughput)
    {
        var timeProvider = new Mock<IDateTimeProvider>();
        var now = DateTime.UtcNow;
        timeProvider.Setup(x => x.GetCurrentTimeUtc()).Returns(now);
        var resource = new Resource<JobThroughputTests, Size>("Test Resource", int.MaxValue, Size.FromLong(long.MaxValue), timeProvider.Object);
        
        using var job = await resource.BeginAsync("Test Job", Size.FromLong(jobSize), default);
        
        // Simulate some workload.
        await job.ReportAsync(Size.FromLong(processed), default);
        timeProvider.Setup(x => x.GetCurrentTimeUtc()).Returns(now.AddSeconds(timeElapsed));
        
        // Check the throughput.
        var throughput = job.GetThroughput(timeProvider.Object);
        throughput.Should().Be(Size.FromLong(expectedThroughput));

        job.Dispose();
        
        // Throughput should be zero after the job is finished.
        throughput = job.GetThroughput(timeProvider.Object);
        throughput.Should().Be(Size.FromLong(0));
    }
    
    [Theory]
    [InlineData(100, 1, 1f, 10, 10)]
    [InlineData(100, 5, 1f, 5, 25)]
    [InlineData(100, 50, 2f, 2, 50)]
    [InlineData(100, 7, 2f, 4, 12)]
    public async Task ReportsCorrectThroughput_WithMultipleJobs(int jobSize, int processed, float timeElapsed, int numJobs, int expectedThroughput)
    {
        var startTimeProvider = new Mock<IDateTimeProvider>();
        var currentTimeProvider = new Mock<IDateTimeProvider>();
        var now = DateTime.UtcNow;
        
        startTimeProvider.Setup(x => x.GetCurrentTimeUtc()).Returns(now);
        currentTimeProvider.Setup(x => x.GetCurrentTimeUtc()).Returns(now.AddSeconds(timeElapsed));
        var resource = new Resource<JobThroughputTests, Size>("Test Resource", int.MaxValue, Size.FromLong(long.MaxValue), startTimeProvider.Object);

        var jobs = new IJob<JobThroughputTests, Size>[numJobs];
        for (var x = 0; x < numJobs; x++)
        {
            // Start and simulate workloads.
            jobs[x] = await resource.BeginAsync("Test Job", Size.FromLong(jobSize), default);
            await jobs[x].ReportAsync(Size.FromLong(processed), default);
        }
        
        // Assert Total Work Done
        var throughput = jobs.GetTotalThroughput(currentTimeProvider.Object);
        throughput.Should().Be(Size.FromLong(expectedThroughput));
        
        // Check the throughput.
        for (var x = 0; x < numJobs; x++)
            resource.Finish(jobs[x]);
        
        // Throughput should be zero after the job is finished.
        var throughputAfterFinish = jobs.GetTotalThroughput(currentTimeProvider.Object);
        throughputAfterFinish.Should().Be(Size.FromLong(0));
    }
}
