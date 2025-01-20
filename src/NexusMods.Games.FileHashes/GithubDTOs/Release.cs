using System.Text.Json.Serialization;

namespace NexusMods.Games.FileHashes.GithubDTOs;

/// <summary>
/// A GitHub release.
/// </summary>
public record Release
{
    /// <summary>
    /// The name of the release.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// The associated tag name of the release.
    /// </summary>
    [JsonPropertyName("tag_name")]
    public required string TagName { get; init; }
    
    /// <summary>
    /// The published date of the release.
    /// </summary>
    [JsonPropertyName("published_at")]
    public required DateTimeOffset PublishedAt { get; init; }
    
    /// <summary>
    /// The created date of the release.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }
    
    /// <summary>
    /// The assets attached to the release.
    /// </summary>
    [JsonPropertyName("assets")]
    public required Asset[] Assets { get; init; }
}
