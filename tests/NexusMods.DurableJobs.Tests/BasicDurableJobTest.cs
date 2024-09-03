using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.DurableJobs;
using NexusMods.Abstractions.Serialization;
using NexusMods.Extensions.BCL;

namespace NexusMods.DurableJobs.Tests;

public class BasicDurableJobTest
{
    private IHost _host;
    private IServiceProvider _serviceProvider;
    private IJobManager _jobManager;
    private readonly InMemoryJobStore _store;

    public BasicDurableJobTest()
    {
        _store = new InMemoryJobStore();
        StartHost();
    }

    private void RestartHost()
    {
        _host.StopAsync();
        _host.Dispose();
        
        StartHost();
    }

    private void StartHost()
    {
        var host = new HostBuilder()
            .ConfigureServices(s => 
                s.AddSerializationAbstractions()
                    .AddDurableJobs()
                    .AddSingleton<IJobStateStore>(s => _store)
                    .AddJob<SquareJob>()
                    .AddJob<SumJob>()
                    .AddJob<WaitMany>()
                    .AddJob<CatchErrorJob>()
                    .AddJob<ThrowOn5Job>()
                    .AddJob<AsyncLinqJob>()
                    .AddJob<WaitFor10>()
                    .AddJob<ManuallyFinishedJob>()
                    .AddUnitOfWorkJob<ManuallyFinishedUnitOfWork>()
                    .AddUnitOfWorkJob<LongRunningUnitOfWork>()
            ).Build();
        _host = host;
        _serviceProvider = host.Services;
        
        _jobManager = _serviceProvider.GetRequiredService<IJobManager>();
    }

    [Fact]
    public async Task CanRunSumOfSquaresJob()
    {
        var values = new[] { 1, 4, 3, 7, 42 };

        var result = await _jobManager.RunNew<SumJob>(values);

        result.Should().Be(values.Select(x => x * x).Sum());
    }

    [Fact]
    public async Task CanWaitAll()
    {
        var values = new [] { 1, 2, 3, 4, 5 };
        
        var result = await _jobManager.RunNew<WaitMany>(values);
        
        result.Should().Be(values.Select(x => x * x).Sum());
    }

    [Fact]
    public async Task SubJobsThrowErrors()
    {
        var values = 10;
        
        Func<Task> act = async () => await _jobManager.RunNew<CatchErrorJob>(values);
        
        await act.Should().ThrowAsync<SubJobError>().WithMessage("I don't like 5");
    }

    [Fact]
    public async Task AsyncLinqWorks()
    {
        var values = new[] { 1, 4, 3, 7, 42 };
        var result = await _jobManager.RunNew<AsyncLinqJob>(values);
        result.Should().Be(values.Select(x => x * x).Sum());
    }

    [Fact]
    public async Task CanRunUnitOfWork()
    {
        var result = (int)await _jobManager.RunNew<WaitFor10>(100);
        
        result.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task JobsRestart()
    {
        // Create a job that will never finish
        _ = _jobManager.RunNew<ManuallyFinishedJob>(1);
        await Task.Delay(200);
        
        // Restart the host
        RestartHost();
        
        // Finish the unit of work
        ManuallyFinishedUnitOfWork.Tcs.SetResult(42);
        
        await Task.Delay(200);
        
        // Check that the job finished
        ManuallyFinishedJob.LastResult.Should().Be(43);
    }
    
}


public class CatchErrorJob : AOrchestration<CatchErrorJob, int, int>
{
    protected override async Task<int> Run(Context context, int arg1)
    {
        var sum = 0;
        for (var i = 0; i < arg1; i++)
        {
            sum += await ThrowOn5Job.RunSubJob(context, i);
        }
        return sum;
    }
}

public class ThrowOn5Job : AOrchestration<ThrowOn5Job, int, int>
{
    protected override Task<int> Run(Context context, int arg1)
    {
        if (arg1 == 5)
        {
            throw new Exception("I don't like 5");
        }

        return Task.FromResult(arg1);
    }
}

public class WaitMany : AOrchestration<WaitMany, int, int[]>
{
    protected override async Task<int> Run(Context context, int[] inputs)
    {
        var tasks = new List<Task<int>>();
        foreach (var input in inputs)
        {
            tasks.Add(SquareJob.RunSubJob(context, input));
        }

        await Task.WhenAll(tasks);

        return tasks.Select(t => t.Result).Sum();
    }
}

public class AsyncLinqJob : AOrchestration<AsyncLinqJob, int, int[]>
{
    protected override async Task<int> Run(Context context, int[] ints)
    {
        var sum = 0;
        await foreach (var val in ints.SelectAsync(async x => await SquareJob.RunSubJob(context, x)))
        {
            sum += val;
        }
        return sum;
    }
}

public class SquareJob : AOrchestration<SquareJob, int, int>
{
    protected override Task<int> Run(Context context, int arg1)
    {
        return Task.FromResult(arg1 * arg1);
    }
}

public class SumJob : AOrchestration<SumJob, int, int[]>
{
    protected override async Task<int> Run(Context context, int[] ints)
    {
        var acc = 0;

        foreach (var val in ints)
        {
            acc += await SquareJob.RunSubJob(context, val);
        }
        
        return acc;
    }
}


public class WaitFor10 : AOrchestration<WaitFor10, int, int>
{
    protected override async Task<int> Run(Context context, int arg1)
    {
        var totalTook = 0;
        for (var i = 0; i < 10; i++)
        {
            totalTook += await LongRunningUnitOfWork.RunUnitOfWork(context, 100);
        }
        return totalTook;
    }
}

/// <summary>
/// A unit of work that delays for the given number of ms, then returns the number of ms it took.
/// </summary>
public class LongRunningUnitOfWork : AUnitOfWork<LongRunningUnitOfWork, int, int>
{
    protected override async Task<int> Start(int maxTime, CancellationToken token)
    {
        var start = DateTime.Now;
        await Task.Delay(maxTime, token);
        return (DateTime.Now - start).Milliseconds;
    }
}

/// <summary>
/// A job that sets the static result when it's done.
/// </summary>
public class ManuallyFinishedJob : AOrchestration<ManuallyFinishedJob, int, int>
{
    public static int LastResult { get; private set; }
    protected override async Task<int> Run(Context context, int arg1)
    {
        LastResult = await ManuallyFinishedUnitOfWork.RunUnitOfWork(context, arg1);
        return LastResult;
    }
}

/// <summary>
/// A test unit of work that finishes when a task completion source is set.
/// </summary>
public class ManuallyFinishedUnitOfWork : AUnitOfWork<ManuallyFinishedUnitOfWork, int, int>
{
    public static readonly TaskCompletionSource<int> Tcs = new();
    protected override async Task<int> Start(int input, CancellationToken token)
    {
        return await Tcs.Task + input;
    }
}
