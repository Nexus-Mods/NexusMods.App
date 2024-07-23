using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using Xunit;

namespace NexusMods.Jobs.Tests;

public partial class SynchronousTests
{
    [Fact]
    public async Task TestCompletedJob()
    {
        var job = new MyJob();
        var worker = new MyWorker();

        await worker.StartAsync(job);
        var result = await job.WaitToFinishAsync();
        result.TryGetCancelled(out _).Should().BeFalse();
        result.TryGetFailed(out _).Should().BeFalse();
        result.TryGetCompleted(out var completed).Should().BeTrue();

        completed!.TryGetData<string>(out var data).Should().BeTrue();
        data.Should().Be("hello world");
    }
}

file class MyWorker : AJobWorker<SynchronousTests.MyJob>
{
    protected override Task<JobResult> ExecuteAsync(SynchronousTests.MyJob job, CancellationToken cancellationToken)
    {
        return Task.FromResult(JobResult.CreateCompleted("hello world"));
    }
}
