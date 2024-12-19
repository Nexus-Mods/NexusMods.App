using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GOG.DTOs;

/// <summary>
/// Information about a build, which is a collection of depots.
/// </summary>
[UsedImplicitly]
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

/// <summary>
/// Details about a depot in a build.
/// </summary>
[UsedImplicitly]
public class BuildDetailsDepot
{
    /// <summary>
    /// The size of the depot when compressed.
    /// </summary>
    [JsonPropertyName("compressedSize")]
    public required Size CompressedSize { get; init; }
    
    /// <summary>
    /// The languages supported by the depot.
    /// </summary>
    [JsonPropertyName("languages")]
    public string[] Languages { get; init; } = [];
    
    /// <summary>
    /// The unique ID of the manifest for this depot.
    /// </summary>
    [JsonPropertyName("manifest")]
    public required string Manifest { get; init; }

    /// <summary>
    /// The OS bitness (32, 64) supported by the depot.
    /// </summary>
    [JsonPropertyName("osBitness")] 
    public int[] OSBitness { get; init; } = [];
    
    /// <summary>
    /// The product ID of the depot.
    /// </summary>
    [JsonPropertyName("productId")]
    public required ProductId ProductId { get; init; }
    
    /// <summary>
    /// The size of the depot when uncompressed.
    /// </summary>
    [JsonPropertyName("size")]
    public required Size Size { get; init; }
}
