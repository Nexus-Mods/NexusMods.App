using System.Text.Json.Serialization;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes.GithubDTOs;

/// <summary>
/// A single asset in a GitHub release.
/// </summary>
public record Asset
{
    /// <summary>
    /// The name of the asset.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// The direct HTTP URL to download the asset.
    /// </summary>
    [JsonPropertyName("browser_download_url")]
    public required Uri BrowserDownloadUrl { get; init; }
    
    /// <summary>
    /// The size of the asset in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public required Size Size { get; init; }
}
