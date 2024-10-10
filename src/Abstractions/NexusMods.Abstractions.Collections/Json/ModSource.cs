using System.Text.Json.Serialization;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Collections.Json;

public class ModSource
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ModSourceType Type { get; init; } 
    
    [JsonPropertyName("modId")]
    public ModId ModId { get; init; }
    
    [JsonPropertyName("fileId")]
    public FileId FileId { get; init; }
    
    [JsonPropertyName("fileSize")]
    public Size FileSize { get; init; }
    
    [JsonPropertyName("fileExpression")]
    public RelativePath FileExpression { get; init; } = default;
}
