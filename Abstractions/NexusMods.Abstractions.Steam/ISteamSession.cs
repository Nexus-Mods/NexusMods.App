using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;

namespace NexusMods.Abstractions.Steam;

/// <summary>
/// An abstraction for a Steam session.
/// </summary>
public interface ISteamSession
{
    /// <summary>
    /// Get the product info for the specified app ID
    /// </summary>
    public Task<ProductInfo> GetProductInfoAsync(AppId appId, CancellationToken cancellationToken = default);
}
