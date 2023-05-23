using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi.DTOs.OAuth;

public class PermissionMap
{
    [JsonPropertyName("collection")]
    public string[] Collection { get; set; } = Array.Empty<string>();

    [JsonPropertyName("collection_image")]
    public string[] CollectionImage { get; set; } = Array.Empty<string>();

    [JsonPropertyName("mod")]
    public string[] Mod { get; set; } = Array.Empty<string>();

    [JsonPropertyName("admin")]
    public string[] Admin { get; set; } = Array.Empty<string>();

    [JsonPropertyName("tag")]
    public string[] Tag { get; set; } = Array.Empty<string>();

    [JsonPropertyName("comment")]
    public string[] Comment { get; set; } = Array.Empty<string>();
}
