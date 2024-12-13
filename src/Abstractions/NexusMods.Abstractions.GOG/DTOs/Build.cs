using System.ComponentModel;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.GOG.JsonConverters;
using NexusMods.Abstractions.GOG.Values;

namespace NexusMods.Abstractions.GOG.DTOs;

public class Build
{
    [JsonPropertyName("build_id")]
    public required BuildId BuildId { get; init; }
    
    [JsonPropertyName("product_id")]
    public required ProductId ProductId { get; init; }
    
    [JsonPropertyName("os")]
    public required string OS { get; init; }
    
    [JsonPropertyName("version_name")]
    public required string VersionName { get; init; }
    
    [JsonPropertyName("tags")]
    public required string[] Tags { get; init; }
    
    [JsonPropertyName("public")]
    public bool Public { get; init; }
    
    [JsonConverter(typeof(GOGDateTimeOffsetConverter))]
    [JsonPropertyName("date_published")]
    public DateTimeOffset DatePublished { get; init; }
    
    [JsonPropertyName("generation")]
    public int Generation { get; init; }
    
    [JsonPropertyName("link")]
    public required Uri? Link { get; init; }
}
