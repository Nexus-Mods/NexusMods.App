using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OneOf;

namespace NexusMods.Abstractions.Jobs;
using Union = OneOf<JobResultCompleted, JobResultCancelled, JobResultFailed>;

/// <summary>
/// Result of a job.
/// </summary>
[PublicAPI]
public class JobResult
{
    private readonly Union _value;

    /// <summary>
    /// Gets the result type.
    /// </summary>
    public JobResultType ResultType { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public JobResult(Union value)
    {
        _value = value;

        ResultType = value.Match(
            f0: _ => JobResultType.Completed,
            f1: _ => JobResultType.Cancelled,
            f2: _ => JobResultType.Failed
        );
    }

    /// <summary>
    /// Returns the result as a <see cref="JobResultCompleted"/> using the try-get pattern.
    /// </summary>
    public bool TryGetCompleted([NotNullWhen(true)] out JobResultCompleted? completed)
    {
        if (ResultType != JobResultType.Completed)
        {
            completed = null;
            return false;
        }

        completed = _value.AsT0;
        return true;
    }

    /// <summary>
    /// Returns the result as a <see cref="JobResultCancelled"/> using the try-get pattern.
    /// </summary>
    public bool TryGetCancelled([NotNullWhen(true)] out JobResultCancelled? cancelled)
    {
        if (ResultType != JobResultType.Cancelled)
        {
            cancelled = null;
            return false;
        }

        cancelled = _value.AsT1;
        return true;
    }

    /// <summary>
    /// Returns the result as a <see cref="JobResultFailed"/> using the try-get pattern.
    /// </summary>
    public bool TryGetFailed([NotNullWhen(true)] out JobResultFailed? failed)
    {
        if (ResultType != JobResultType.Failed)
        {
            failed = null;
            return false;
        }

        failed = _value.AsT2;
        return true;
    }

    /// <summary>
    /// Returns the data in the completed result or thrown an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result isn't of type <see cref="JobResultCompleted{TData}"/></exception>
    public TData RequireData<TData>()
    where TData : notnull
    {
        if (!TryGetCompleted(out var completed))
            throw new InvalidOperationException($"JobResult is of type `{ResultType}` but expected `{JobResultType.Completed}`: `{ToString()}`");

        if (!completed.TryGetData<TData>(out var data))
            throw new InvalidOperationException($"Completed JobResult doesn't have data: `{ToString()}`");

        return data;
    }

    /// <summary>
    /// Creates a failed job result from an exception.
    /// </summary>
    [StackTraceHidden]
    public static JobResult CreateFailed(Exception exception)
    {
        return new JobResult(new JobResultFailed
        {
            Exception = exception,
        });
    }

    /// <summary>
    /// Creates a failed job result from an error message.
    /// </summary>
    [StackTraceHidden]
    public static JobResult CreateFailed(string message)
    {
        return CreateFailed(new Exception(message));
    }

    /// <summary>
    /// Creates a cancelled job result.
    /// </summary>
    [StackTraceHidden] public static JobResult CreateCancelled() => new(new JobResultCancelled());

    /// <summary>
    /// Creates a completed job result with data.
    /// </summary>
    [StackTraceHidden]
    public static JobResult CreateCompleted<TData>(TData data)
        where TData : notnull
    {
        return new JobResult(new JobResultCompleted<TData>
        {
            Data = data,
        });
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return _value.Match(
            f0: x => x.ToString(),
            f1: x => x.ToString(),
            f2: x => x.ToString()
        );
    }
}
