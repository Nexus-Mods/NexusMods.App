using System.Text.Json.Serialization;

namespace NexusMods.Updater.DTOs;

public class Release
{
    [JsonPropertyName("tag_name")] public string Tag { get; set; } = "";
    [JsonPropertyName("assets")] public Asset[] Assets { get; set; } = Array.Empty<Asset>();
}
