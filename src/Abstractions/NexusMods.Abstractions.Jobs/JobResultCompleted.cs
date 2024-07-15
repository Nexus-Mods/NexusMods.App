using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Result of a completed job.
/// </summary>
[PublicAPI]
public record JobResultCompleted
{
    public bool TryGetData<TData>([NotNullWhen(true)] out TData? data)
        where TData : notnull
    {
        if (this is not JobResultCompleted<TData> resultWithData)
        {
            data = default(TData);
            return false;
        }

        data = resultWithData.Data;
        return true;
    }
}

/// <summary>
/// Result of a completed job with data.
/// </summary>
/// <typeparam name="TData"></typeparam>
[PublicAPI]
public record JobResultCompleted<TData> : JobResultCompleted where TData : notnull
{
    /// <summary>
    /// Gets the data of the completed job.
    /// </summary>
    public required TData Data { get; init; }
}

