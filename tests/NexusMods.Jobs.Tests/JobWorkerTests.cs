using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using Xunit;

namespace NexusMods.Jobs.Tests;

public class JobWorkerTests(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task TestCreateSync()
    {
        var job = new MyJob();
        var worker = jobMonitor.Begin<MyJob, string>(job, async _ => "hello world");

        var jobResult = await worker;
        jobResult.Should().Be("hello world");
    }

    [Fact]
    public async Task TestCreateAsync()
    {
        var job = new MyJob();
        var worker = jobMonitor.Begin(job, async _ =>
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
