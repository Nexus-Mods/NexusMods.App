using System.Text.Json.Serialization;

namespace NexusMods.Games.FileHashes.GithubDTOs;

/// <summary>
/// A GitHub asset, an uploaded file
/// </summary>
public class Asset
{
    /// <summary>
    /// The name of the asset.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The download URL for the asset.
    /// </summary>
    [JsonPropertyName("browser_download_url")]
    public required Uri BrowserDownloadUrl { get; init; }
}
