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

public static partial class Extensions
{
    /// <summary>
    /// Cast the data from <typeparamref name="TCurrent"/> to <typeparamref name="TOther"/>.
    /// </summary>
    public static Resource<TOther> Cast<TCurrent, TOther>(this Resource<TCurrent> resource)
        where TCurrent : TOther
        where TOther : notnull
    {
        return new Resource<TOther>
        {
            Data = resource.Data,
            ExpiresAt = resource.ExpiresAt,
        };
    }
}
