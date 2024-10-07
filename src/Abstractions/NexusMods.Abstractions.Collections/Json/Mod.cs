using System.Text.Json.Serialization;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

namespace NexusMods.Abstractions.Collections.Json;

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
}
