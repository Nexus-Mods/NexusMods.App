using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.Collections.Json;

public class CollectionRoot
{
    [JsonPropertyName("info")]
    public required CollectionInfo Info { get; init; }
    
    [JsonPropertyName("mods")]
    public Mod[] Mods { get; init; } = [];
}
