using System.Text.Json.Serialization;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Abstractions.Collections.Json;

public class CollectionInfo
{
    [JsonPropertyName("author")]
    public string Author { get; init; } = string.Empty;
    
    [JsonPropertyName("authorUrl")]
    public string AuthorUrl { get; init; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    [JsonPropertyName("installInstructions")]
    public string InstallInstructions { get; init; } = string.Empty;
    
    [JsonPropertyName("domainName")]
    public required GameDomain DomainName { get; init; }
    
    [JsonPropertyName("gameVersions")]
    public string[] GameVersions { get; init; } = [];
}
