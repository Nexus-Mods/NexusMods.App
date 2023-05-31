using FluentAssertions;
using Moq;
using NexusMods.Paths;

namespace NexusMods.DataModel.RateLimiting.Tests;

public class ThroughputTests
{
    // Note: This new system is not super duper accurate (no floating point math for sizes), but it's good enough for our purposes
    //       given that we do the counting in bytes.
    [Theory]
    [InlineData(100, 1, 1f, 1)]
    [InlineData(100, 5, 1f, 5)]
    [InlineData(100, 50, 2f, 25)]
    [InlineData(100, 7, 2f, 3)]
    public async Task JobCanReportCorrectThroughput(int jobSize, int processed, float timeElapsed, int expectedThroughput)
    {
        var timeProvider = new Mock<IDateTimeProvider>();
        var now = DateTime.UtcNow;
        timeProvider.Setup(x => x.GetCurrentTimeUtc()).Returns(now);
        var resource = new Resource<ThroughputTests, Size>("Test Resource", int.MaxValue, Size.FromLong(long.MaxValue), timeProvider.Object);
        
        // Note: We're not creating job through resource because we want more control over time.
        var job = await resource.BeginAsync("Test Job", Size.FromLong(jobSize), default);
        
        // Simulate some workload.
        await job.ReportAsync(Size.FromLong(processed), default);
        timeProvider.Setup(x => x.GetCurrentTimeUtc()).Returns(now.AddSeconds(timeElapsed));
        
        // Check the throughput.
        var throughput = job.GetThroughput(timeProvider.Object);
        throughput.Should().Be(Size.FromLong(expectedThroughput));

        resource.Finish(job); 
    }
}
