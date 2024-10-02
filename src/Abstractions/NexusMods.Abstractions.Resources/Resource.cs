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
    public DateTime ExpiresAt { get; init; } = DateTime.MaxValue;

    /// <summary>
    /// Creates a new resource.
    /// </summary>
    public Resource<TOther> WithData<TOther>(TOther data) where TOther : notnull
    {
        return new Resource<TOther>
        {
            Data = data,
            ExpiresAt = ExpiresAt,
        };
    }
}
