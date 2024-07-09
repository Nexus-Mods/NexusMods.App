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
}
