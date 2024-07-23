using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Result of a failed job.
/// </summary>
[PublicAPI]
public record JobResultFailed
{
    /// <summary>
    /// Gets the exception that resulted in failure.
    /// </summary>
    public required Exception Exception { get; init; }
}
