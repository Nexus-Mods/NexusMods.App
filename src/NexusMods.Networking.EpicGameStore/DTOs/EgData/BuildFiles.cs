using System.Text.Json.Serialization;

namespace NexusMods.Networking.EpicGameStore.DTOs.EgData;

public class BuildFiles
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("files")]
    public BuildFile[] Files { get; set; } = [];
}
