using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using Xunit;

namespace NexusMods.Jobs.Tests;

public class SynchronousTests(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task TestCompletedJob()
    {
        var job = jobMonitor.Begin<MyLocalJob, string>(new MyLocalJob());

        (await job).Should().Be("hello world");
    }
}

file class MyLocalJob : IJobDefinitionWithStart<MyLocalJob, string>
{
    public ValueTask<string> StartAsync(IJobContext<MyLocalJob> context)
    {
        return new ValueTask<string>("hello world");
    }
}
