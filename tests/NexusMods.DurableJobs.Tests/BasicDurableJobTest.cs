using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.DurableJobs;
using NexusMods.Extensions.BCL;

namespace NexusMods.DurableJobs.Tests;

public class BasicDurableJobTest
{
    private readonly IHost _host;
    private readonly IServiceProvider _serviceProvider;
    private readonly IJobManager _jobManager;

    public BasicDurableJobTest()
    {
        var host = new HostBuilder()
            .ConfigureServices(s => 
                s.AddDurableJobs()
                .AddSingleton<SquareJob>()
                .AddSingleton<SumJob>()
                .AddSingleton<WaitMany>()
                .AddSingleton<CatchErrorJob>()
                .AddSingleton<ThrowOn5Job>()
                .AddSingleton<AsyncLinqJob>()
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
    
}


public class CatchErrorJob : AJob<CatchErrorJob, int, int>
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

public class ThrowOn5Job : AJob<ThrowOn5Job, int, int>
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

public class WaitMany : AJob<WaitMany, int, int[]>
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

public class AsyncLinqJob : AJob<AsyncLinqJob, int, int[]>
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

public class SquareJob : AJob<SquareJob, int, int>
{
    protected override Task<int> Run(Context context, int arg1)
    {
        return Task.FromResult(arg1 * arg1);
    }
}

public class SumJob : AJob<SumJob, int, int[]>
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
