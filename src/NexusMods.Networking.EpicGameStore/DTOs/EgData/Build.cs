using System.Text.Json.Serialization;

namespace NexusMods.Networking.EpicGameStore.DTOs.EgData;

public class Build
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("appName")]
    public string AppName { get; set; } = string.Empty;
    
    /// <summary>
    /// The manifest hash of the build.
    /// </summary>
    [JsonPropertyName("hash")]
    public string ManifestHash { get; set; } = string.Empty;
    
    [JsonPropertyName("labelName")]
    public string LabelName { get; set; } = string.Empty;
    
    [JsonPropertyName("buildVersion")]
    public string BuildVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.MinValue;
    
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.MinValue;
}
