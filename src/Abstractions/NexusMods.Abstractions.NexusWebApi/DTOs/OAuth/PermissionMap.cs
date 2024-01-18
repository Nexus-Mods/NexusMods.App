using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;

// TODO: This needs better docs; need to ask the backend team, OAuth isn't really publicly documented anywhere.

/// <summary>
/// Represents the permissions provided by an OAuth JWT token provided by the Nexus Mods API.
/// Each array represents the permissions the user has for a specific entity.
/// </summary>
public class PermissionMap
{    
    /// <summary>
    /// Gets or sets the user's permissions on collections.
    /// </summary>
    [JsonPropertyName("collection")]
    public string[] Collection { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the user's permissions on collection images.
    /// </summary>
    [JsonPropertyName("collection_image")]
    public string[] CollectionImage { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the user's permissions on mods.
    /// </summary>
    [JsonPropertyName("mod")]
    public string[] Mod { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the user's admin permissions.
    /// </summary>
    [JsonPropertyName("admin")]
    public string[] Admin { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets the user's permissions on tags.
    /// </summary>
    [JsonPropertyName("tag")]
    public string[] Tag { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets the user's permissions on comments.
    /// </summary>
    [JsonPropertyName("comment")]
    public string[] Comment { get; set; } = Array.Empty<string>();
}
