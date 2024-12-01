using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using Xunit;

namespace NexusMods.Jobs.Tests;

public class JobWorkerTests
{
    [Fact]
    public async Task TestCreateSync()
    {
        var monitor = new JobMonitor();
        var job = new MyJob();
        var worker = monitor.Begin<MyJob, string>(job, async _ => "hello world");

        var jobResult = await worker;
        jobResult.Should().Be("hello world");
    }

    [Fact]
    public async Task TestCreateAsync()
    {
        var monitor = new JobMonitor();
        var job = new MyJob();
        var worker = monitor.Begin(job, async _ =>
            {
                await Task.Delay(100);
                return "hello world";
            }
        );
        
        var jobResult = await worker;
        jobResult.Should().Be("hello world");
    }

    private class MyJob : IJobDefinition<string>;
}
