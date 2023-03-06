using System.Diagnostics;
using FluentAssertions;
using NexusMods.Paths;
using static System.Threading.Tasks.Task;

namespace NexusMods.DataModel.RateLimiting.Tests;

public class RateLimiterTests
{
    [Fact]
    public async Task BasicTaskTests()
    {
        var rateLimiter = new Resource<RateLimiterTests, Size>("Test Resource", 2, Size.Zero);

        var current = 0;
        var max = 0;
        object lockObj = new();

        void SetMax(object o, ref int i, ref int max1, int add)
        {
            lock (o)
            {
                i += add;
                max1 = Math.Max(i, max1);
            }
        }

        await Parallel.ForEachAsync(Enumerable.Range(0, 100),
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (_, token) =>
            {
                using var job = await rateLimiter.Begin("Incrementing", Size.One, CancellationToken.None);
                SetMax(lockObj, ref current, ref max, 1);
                await Delay(10, token);
                SetMax(lockObj, ref current, ref max, -1);
            });

        Assert.Equal(2, max);
    }

    [Fact]
    public async Task TestBasicThroughput()
    {
        var rateLimiter = new Resource<RateLimiterTests, Size>("Test Resource", 1, Size.MB);

        using var job = await rateLimiter.Begin("Transferring", Size.MB * 5 / 2, CancellationToken.None);

        var sw = Stopwatch.StartNew();

        var report = rateLimiter.StatusReport;
        Assert.Equal((Size)0L, report.Transferred);
        foreach (var x in Enumerable.Range(0, 5)) await job.Report(Size.MB / 2, CancellationToken.None);

        var elapsed = sw.Elapsed;
        //Assert.True(elapsed > TimeSpan.FromSeconds(1));
        //Assert.True(elapsed < TimeSpan.FromSeconds(3));

        report = rateLimiter.StatusReport;
        //Assert.Equal((Size)(1024L * 1024 * 5 / 2), report.Transferred);
    }

    [Fact]
    public async Task TestParallelThroughput()
    {
        var rateLimiter = new Resource<RateLimiterTests, Size>("Test Resource", 2, Size.MB);


        var sw = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (var i = 0; i < 4; i++)
            tasks.Add(Run(async () =>
            {
                using var job = await rateLimiter.Begin("Transferring", Size.MB / 10 * 5, CancellationToken.None);
                for (var x = 0; x < 5; x++) await job.Report(Size.MB / 10, CancellationToken.None);
            }));

        await WhenAll(tasks.ToArray());
        var elapsed = sw.Elapsed;
        //Assert.True(elapsed > TimeSpan.FromSeconds(1));
        //Assert.True(elapsed < TimeSpan.FromSeconds(6));
    }

    [Fact]
    public async Task TestParallelThroughputWithLimitedTasks()
    {
        var rateLimiter = new Resource<RateLimiterTests, Size>("Test Resource", 1, Size.MB * 4);
        ;

        var sw = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (var i = 0; i < 4; i++)
            tasks.Add(Run(async () =>
            {
                using var job = await rateLimiter.Begin("Transferring", Size.MB / 10 * 5L, CancellationToken.None);
                for (var x = 0; x < 5; x++) await job.Report(Size.MB / 10, CancellationToken.None);
            }));

        await WhenAll(tasks.ToArray());
        var elapsed = sw.Elapsed;
        elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(0.25));
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5.5));
    }

    [Fact]
    public async Task CanGetJobStatus()
    {
        var rateLimiter = new Resource<RateLimiterTests, Size>("Test Resource");
        var job = await rateLimiter.Begin("Test Job", Size.KB, CancellationToken.None);

        job.Started.Should().BeTrue();
        job.Size.Should().Be(Size.KB);
        job.Resource.Should().Be(rateLimiter);
        job.Description.Should().Be("Test Job");
        job.Progress.Should().Be(Percent.Zero);
        job.ReportNoWait(Size.KB / 2);
        job.Progress.Should().Be(Percent.FactoryPutInRange(0.50));
    }
}
