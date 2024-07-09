using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Result of a completed job.
/// </summary>
[PublicAPI]
public record JobResultCompleted;

/// <summary>
/// Result of a completed job with data.
/// </summary>
/// <typeparam name="TData"></typeparam>
[PublicAPI]
public record JobResultCompleted<TData> : JobResultCompleted
{
    /// <summary>
    /// Gets the data of the completed job.
    /// </summary>
    public required TData Data { get; init; }
}

