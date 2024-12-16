using System.Text.Json.Serialization;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GOG.DTOs;

public class BuildDetails
{
    [JsonPropertyName("baseProductId")]
    public required ProductId BaseProductId { get; init; }
    
    [JsonPropertyName("buildId")]
    public required BuildId BuildId { get; init; }

    [JsonPropertyName("dependencies")]
    public string[] Dependencies { get; init; } = [];
    
    [JsonPropertyName("depots")]
    public required BuildDetailsDepot[] Depots { get; init; }
}

public class BuildDetailsDepot
{
    [JsonPropertyName("compressedSize")]
    public required Size CompressedSize { get; init; }
    
    [JsonPropertyName("languages")]
    public string[] Languages { get; init; } = [];
    
    [JsonPropertyName("manifest")]
    public required string Manifest { get; init; }
    
    [JsonPropertyName("osBitness")]
    public required int[] OSBitness { get; init; }
    
    [JsonPropertyName("productId")]
    public required ProductId ProductId { get; init; }
    
    [JsonPropertyName("size")]
    public required Size Size { get; init; }
}
