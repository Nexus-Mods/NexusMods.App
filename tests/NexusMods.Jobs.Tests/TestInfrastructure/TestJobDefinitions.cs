using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace NexusMods.Jobs.Tests.TestInfrastructure;

public record SimpleTestJob(ManualResetEventSlim? StartSignal = null) : IJobDefinitionWithStart<SimpleTestJob, string>
{
    public async ValueTask<string> StartAsync(IJobContext<SimpleTestJob> context)
    {
        // Wait for signal before starting checkpoint reporting if provided
        if (StartSignal != null)
            if (!StartSignal.Wait(TimeSpan.FromSeconds(30), context.CancellationToken))
                throw new TimeoutException("StartSignal was not set within timeout period");
        
        await context.YieldAsync();
        context.SetPercent(Size.From(1UL), Size.From(1UL));
        return "Completed";
    }
}

public record ProgressReportingJob(int StepCount, TimeSpan StepDelay, ManualResetEventSlim? StartSignal = null) : IJobDefinitionWithStart<ProgressReportingJob, double>
{
    public async ValueTask<double> StartAsync(IJobContext<ProgressReportingJob> context)
    {
        // Wait for signal before starting progress reporting if provided
        if (StartSignal != null)
            if (!StartSignal.Wait(TimeSpan.FromSeconds(30), context.CancellationToken))
                throw new TimeoutException("StartSignal was not set within timeout period");
        
        for (var x = 0; x < StepCount; x++)
        {
            await Task.Delay(StepDelay);
            await context.YieldAsync();

            context.SetPercent(Size.FromLong(x + 1), Size.FromLong(StepCount));
            if (x <= 0) continue;
            var rate = 1.0 / StepDelay.TotalSeconds;
            context.SetRateOfProgress(rate);
        }
        return 1.0;
    }
}

public record SignaledJob(ManualResetEventSlim StartSignal, ManualResetEventSlim? CompletionSignal = null) : IJobDefinitionWithStart<SignaledJob, bool>
{
    public async ValueTask<bool> StartAsync(IJobContext<SignaledJob> context)
    {
        // Use timeout to prevent infinite hanging if signal is never set
        if (!StartSignal.Wait(TimeSpan.FromSeconds(30), context.CancellationToken))
            throw new TimeoutException("StartSignal was not set within timeout period");

        await context.YieldAsync();
        CompletionSignal?.Set();
        return true;
    }
}

public record FailingJob(Exception ExceptionToThrow, ManualResetEventSlim? FailSignal = null) : IJobDefinitionWithStart<FailingJob, string>
{
    public async ValueTask<string> StartAsync(IJobContext<FailingJob> context)
    {
        await context.YieldAsync();
        context.SetPercent(Size.From(1UL), Size.From(3UL));
        
        // Wait for signal before failing if provided (for race-condition-free testing)
        if (FailSignal == null) throw ExceptionToThrow;
        if (!FailSignal.Wait(TimeSpan.FromSeconds(30), context.CancellationToken))
            throw new TimeoutException("FailSignal was not set within timeout period");

        throw ExceptionToThrow;
    }
}

public record DelayedJob(TimeSpan Delay, ManualResetEventSlim? StartedSignal = null) : IJobDefinitionWithStart<DelayedJob, string>
{
    public async ValueTask<string> StartAsync(IJobContext<DelayedJob> context)
    {
        StartedSignal?.Set();
        await Task.Delay(Delay, context.CancellationToken);
        await context.YieldAsync();
        return "Delay completed";
    }
}

public record WaitForCancellationJob(ManualResetEventSlim ReadySignal) : IJobDefinitionWithStart<WaitForCancellationJob, string>
{
    public async ValueTask<string> StartAsync(IJobContext<WaitForCancellationJob> context)
    {
        ReadySignal.Set();
        
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10, CancellationToken.None);
            await context.YieldAsync();
        }

        return "Should not reach here";
    }
}

public record SelfCancellingJob() : IJobDefinitionWithStart<SelfCancellingJob, string>
{
    public async ValueTask<string> StartAsync(IJobContext<SelfCancellingJob> context)
    {
        await context.YieldAsync();
        context.SetPercent(Size.From(1UL), Size.From(3UL));
        
        context.CancelAndThrow("Job self-cancelled");
        
        return "Should not reach here";
    }
}

public record IndeterminateProgressJob(int ItemCount, TimeSpan ItemDelay, ManualResetEventSlim? StartSignal = null) : IJobDefinitionWithStart<IndeterminateProgressJob, int>
{
    internal double ExpectedRate => 1.0 / ItemDelay.TotalSeconds; // items per second

    public async ValueTask<int> StartAsync(IJobContext<IndeterminateProgressJob> context)
    {
        // Wait for signal before starting if provided
        if (StartSignal != null)
            if (!StartSignal.Wait(TimeSpan.FromSeconds(30), context.CancellationToken))
                throw new TimeoutException("StartSignal was not set within timeout period");

        var processed = 0;

        // For indeterminate progress, use Size.One as maximum to avoid division by zero
        context.SetPercent(Size.Zero, Size.One);

        // Process items as they come (unknown total work)
        for (var x = 0; x < ItemCount; x++)
        {
            await Task.Delay(ItemDelay, context.CancellationToken);
            processed++;

            // We may not know the progress, but we know the rate of progress
            context.SetRateOfProgress(ExpectedRate);

            await context.YieldAsync();
        }

        // Final progress when complete
        context.SetPercent(Size.One, Size.One);
        return processed;
    }
}

public record NonYieldingJob(ManualResetEventSlim CompletionSignal, ManualResetEventSlim? StartedSignal = null) : IJobDefinitionWithStart<NonYieldingJob, string>
{
    public ValueTask<string> StartAsync(IJobContext<NonYieldingJob> context)
    {
        StartedSignal?.Set();
        
        // Wait for the completion signal without calling YieldAsync() or using cancellation-aware APIs
        // This simulates a job that never yields and doesn't respect cancellation
        CompletionSignal.Wait();
        
        return ValueTask.FromResult("Non-yielding work completed");
    }
}
