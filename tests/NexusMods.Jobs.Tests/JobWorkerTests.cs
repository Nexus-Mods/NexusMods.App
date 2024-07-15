using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using Xunit;

namespace NexusMods.Jobs.Tests;

public class JobWorkerTests
{
    [Fact]
    public async Task TestCreateSync()
    {
        var job = new MyJob();
        var worker = JobWorker.Create<MyJob, string>(new MyJob(), (_, _, _) => Task.FromResult<string>("hello world"));

        await worker.StartAsync(job);
        var jobResult = await job.WaitToFinishAsync();
        jobResult.TryGetCompleted(out var completed).Should().BeTrue();
        completed!.TryGetData<string>(out var data).Should().BeTrue();
        data.Should().Be("hello world");
    }

    [Fact]
    public async Task TestCreateAsync()
    {
        var job = new MyJob();
        var worker = JobWorker.Create<MyJob, string>(new MyJob(), async (_, _, _) =>
        {
            await Task.Yield();
            return "hello world";
        });

        await worker.StartAsync(job);
        var jobResult = await job.WaitToFinishAsync();
        jobResult.TryGetCompleted(out var completed).Should().BeTrue();
        completed!.TryGetData<string>(out var data).Should().BeTrue();
        data.Should().Be("hello world");
    }

    private class MyJob : AJob
    {
        public MyJob(IJobGroup? group = default, IJobWorker? worker = default)
            : base(null!, group, worker) { }
    }
}
