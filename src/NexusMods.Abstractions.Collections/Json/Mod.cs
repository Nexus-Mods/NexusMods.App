using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

namespace NexusMods.Abstractions.Collections.Json;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Mod
{
    /// <summary>
    /// The name of the mod
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
    
    [JsonPropertyName("optional")]
    public bool Optional { get; init; }
    
    /// <summary>
    ///     TODO: Deprecate this with <see cref="GameId"/>
    /// </summary>
    [JsonPropertyName("domainName")]
    public required GameDomain DomainName { get; init; }
    
    [JsonPropertyName("source")]
    public required ModSource Source { get; init; }

    [JsonPropertyName("hashes")] 
    public PathAndHash[] Hashes { get; init; } = [];
    
    [JsonPropertyName("choices")]
    public Choices? Choices { get; init; }
    
    /// <summary>
    /// Patches for files found in the mod, the string is a path to the file inside the mod's downloaded archive
    /// and the PatchHash is the CRC32 hash of the file before it's patched. The files patched in this way may
    /// be installed later via MD5 hash. If the file appears in the Hashes array.
    /// </summary>
    [JsonPropertyName("patches")]
    public Dictionary<string, PatchHash> Patches { get; init; } = new();

    /// <summary>
    /// Written instruction by the collection author for users.
    /// </summary>
    [JsonPropertyName("instructions")]
    [LanguageInjection("text")]
    public string Instructions { get; init; } = string.Empty;
}
