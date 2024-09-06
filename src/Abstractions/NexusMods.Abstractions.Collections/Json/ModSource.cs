using System.Text.Json.Serialization;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Collections.Json;

public class ModSource
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    
    [JsonPropertyName("modId")]
    public ModId ModId { get; init; }
    
    [JsonPropertyName("fileId")]
    public FileId FileId { get; init; }
    
    [JsonPropertyName("fileSize")]
    public Size FileSize { get; init; }
}
