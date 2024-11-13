using JetBrains.Annotations;

namespace NexusMods.Abstractions.Resources;

/// <summary>
/// Represents a resource.
/// </summary>
[PublicAPI]
public record Resource<TData> where TData : notnull
{
    /// <summary>
    /// Gets the data of the resource.
    /// </summary>
    public required TData Data { get; init; }

    /// <summary>
    /// Gets the expiration date.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; } = DateTimeOffset.MaxValue;

    /// <summary>
    /// Creates a new resource.
    /// </summary>
    public Resource<TOther> WithData<TOther>(TOther data, bool shouldDispose = true) where TOther : notnull
    {
        if (shouldDispose && Data is IDisposable disposableData) disposableData.Dispose();

        return new Resource<TOther>
        {
            Data = data,
            ExpiresAt = ExpiresAt,
        };
    }
}
