using System.Text.Json.Serialization;
using NexusMods.Abstractions.Games.DTO;

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
    
    [JsonPropertyName("domainName")]
    public required GameDomain DomainName { get; init; }
    
    [JsonPropertyName("source")]
    public required ModSource Source { get; init; }
}
