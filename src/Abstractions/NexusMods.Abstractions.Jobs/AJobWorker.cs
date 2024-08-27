using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using DynamicData.Kernel;
using JetBrains.Annotations;
using OneOf;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A base implementation of <see cref="IJobWorker"/>.
/// </summary>
[PublicAPI]
public abstract class AJobWorker : IJobWorker
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    protected abstract Task<JobResult> ExecuteAsync(AJob job, CancellationToken cancellationToken);

    private record struct Paused;

    private async Task<OneOf<JobResult, Paused>> ExecuteAsyncWrapper(AJob job, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteAsync(job, cancellationToken);
            return OneOf<JobResult, Paused>.FromT0(result);
        }
        catch (TaskCanceledException)
        {
            if (job.IsRequestingPause)
            {
                job.IsRequestingPause = false;
                return new Paused();
            }

            return JobResult.CreateCancelled();
        }
        catch (Exception e)
        {
            return JobResult.CreateFailed(e);
        }
    }

    protected void SetWorker(AJob job)
    {
        if (job.Worker is null)
        {
            job.SetWorker(this);
            return;
        }

        Debug.Assert(ReferenceEquals(job.Worker, this), "same worker");
    }

    /// <inheritdoc/>
    public ValueTask StartAsync(IJob input, CancellationToken cancellationToken = default)
    {
        if (input is not AJob job) throw new NotSupportedException();
        cancellationToken.ThrowIfCancellationRequested();

        SetWorker(job);
        if (job.Status == JobStatus.None)
        {
            job.SetStatus(JobStatus.Created);
        }

        if (job.Status == JobStatus.Paused)
        {
            Debug.Assert(job.Task is null || job.Task.IsCompleted, "task should've completed");
            job.Task = null;
            var didReset = job.CancellationTokenSource.TryReset();
            Debug.Assert(didReset, "reset should work");
        }

        job.SetStatus(JobStatus.Running);

        Task<OneOf<JobResult, Paused>> task;
        try
        {
            task = Task.Run(async () =>
            {
                var result = await ExecuteAsyncWrapper(job, job.CancellationTokenSource.Token);
                if (result.IsT0)
                {
                    job.SetResult(result.AsT0, inferStatus: true);
                }
                else
                {
                    job.SetStatus(JobStatus.Paused);
                }

                job.Task = null;
                return result;
            }, cancellationToken: CancellationToken.None);
        }
        catch (Exception e)
        {
            // synchronous or async startup exception
            job.SetResult(JobResult.CreateFailed(e), inferStatus: true);
            return ValueTask.CompletedTask;
        }

        job.Task = task;
        return ValueTask.CompletedTask;
    }

    private static JobResult TaskResultToJobResult(Task<JobResult> task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        if (task.IsCanceled)
        {
            return JobResult.CreateCancelled();
        }

        if (task.IsFaulted)
        {
            return JobResult.CreateFailed(task.Exception);
        }

        throw new UnreachableException();
    }

    /// <inheritdoc/>
    public async ValueTask PauseAsync(IJob input, CancellationToken cancellationToken = default)
    {
        if (input is not AJob job) throw new NotSupportedException();
        cancellationToken.ThrowIfCancellationRequested();
        job.Status.AssertTransition(JobStatus.Paused);

        var task = job.ObservableStatus
            .FirstAsync(status => status == JobStatus.Paused)
            .ToTask(cancellationToken);

        job.IsRequestingPause = true;
        await job.CancellationTokenSource.CancelAsync();
        await task;
    }

    /// <inheritdoc/>
    public async ValueTask CancelAsync(IJob input, CancellationToken cancellationToken = default)
    {
        if (input is not AJob job) throw new NotSupportedException();
        cancellationToken.ThrowIfCancellationRequested();
        job.Status.AssertTransition(JobStatus.Cancelled);

        var task = job.ObservableStatus
            .FirstAsync(status => status == JobStatus.Paused)
            .ToTask(cancellationToken);

        job.IsRequestingPause = false;
        await job.CancellationTokenSource.CancelAsync();
        await task;
    }
}

[PublicAPI]
public abstract class AJobWorker<TJob> : AJobWorker
    where TJob : AJob
{
    /// <summary>
    /// The progress rate formatter used when setting the progress rate, this must be set before .SetProgressRate is called.
    /// </summary>
    protected Optional<IProgressRateFormatter> ProgressRateFormatter { get; set; } = Optional<IProgressRateFormatter>.None;
    
    /// <inheritdoc/>
    protected override Task<JobResult> ExecuteAsync(AJob job, CancellationToken cancellationToken)
    {
        if (job is not TJob expectedJob) throw new NotSupportedException();
        return ExecuteAsync(expectedJob, cancellationToken);
    }

    /// <inheritdoc cref="AJobWorker.ExecuteAsync"/>
    protected abstract Task<JobResult> ExecuteAsync(TJob job, CancellationToken cancellationToken);

    protected DeterminateProgress GetDeterminateProgress(TJob job)
    {
        if (!job.Progress.TryGetDeterminateProgress(out var determinateProgress))
            throw new InvalidOperationException("Job does not have determinate progress");

        return determinateProgress;
    }

    /// <summary>
    /// Sets the absolute progress percentage of the job.
    /// </summary>
    protected void SetProgress(TJob job, Percent progress)
    {
        GetDeterminateProgress(job).SetPercent(progress);
    }

    /// <summary>
    /// Sets the relative progress rate of the job.
    /// </summary>
    protected void SetProgressRate(TJob job, double rate)
    {
        GetDeterminateProgress(job).SetProgressRate(new ProgressRate(rate, ProgressRateFormatter.Value));
    }
}
