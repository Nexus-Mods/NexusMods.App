using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Paths;

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

    /// <summary>
    /// Get the manifest data for a specific manifest
    /// </summary>
    public Task<Manifest> GetManifestContents(AppId appId, DepotId depotId, ManifestId manifestId, string branch, CancellationToken token = default);

    /// <summary>
    /// Get a readable, seekable, stream for the specified file in the specified manifest
    /// </summary>
    public Stream GetFileStream(AppId appId, Manifest manifest, RelativePath file);
}
