using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Collections.Json;

/// <summary>
/// DTO representing the `collection.json` file
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class CollectionRoot
{
    [JsonPropertyName("info")]
    public required CollectionInfo Info { get; init; }
    
    [JsonPropertyName("mods")]
    public Mod[] Mods { get; init; } = [];

    /// <summary>
    /// Vortex-styled rules for "mods".
    /// </summary>
    [JsonPropertyName("modRules")]
    public ModRule[] ModRules { get; init; } = [];
}
