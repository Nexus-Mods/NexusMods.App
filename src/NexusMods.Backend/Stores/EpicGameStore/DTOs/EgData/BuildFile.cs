using System.Text.Json.Serialization;

namespace NexusMods.Backend.Stores.EpicGameStore.DTOs.EgData;

public class BuildFile
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("fileHash")]
    public string FileHash { get; set; } = string.Empty;
    
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; } = 0;
}
