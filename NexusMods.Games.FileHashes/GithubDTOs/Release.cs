using System.Text.Json.Serialization;

namespace NexusMods.Games.FileHashes.GithubDTOs;

/// <summary>
/// Metadata for a release
/// </summary>
public class Release
{
    /// <summary>
    /// the unique identifier of the release
    /// </summary>
    [JsonPropertyName("id")]
    public required long Id { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// The date the release was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// The date the release was published
    /// </summary>
    [JsonPropertyName("published_at")]
    public required DateTimeOffset PublishedAt { get; set; }
    
    /// <summary>
    /// The assets attached to the release.
    /// </summary>
    [JsonPropertyName("assets")]
    public required Asset[] Assets { get; init; } 
}
