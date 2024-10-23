using System.Text.Json.Serialization;
using ExCSS;
using NexusMods.Abstractions.Collections.Types;
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
    
    /// <summary>
    /// MD5 hash a direct download
    /// </summary>
    [JsonPropertyName("md5")]
    public Md5HashValue Md5 { get; init; }
    
    /// <summary>
    /// If this is a direct download, this is the URL to download the mod from
    /// </summary>
    [JsonPropertyName("url")]
    public Url? Url { get; init; }

    /// <summary>
    /// The name of the mod in the installed loadout
    /// </summary>
    [JsonPropertyName("logicalFilename")]
    public string? LogicalFilename { get; init; }

    [JsonPropertyName("fileId")]
    public FileId FileId { get; init; }
    
    [JsonPropertyName("fileSize")]
    public Size FileSize { get; init; }
    
    [JsonPropertyName("fileExpression")]
    public RelativePath FileExpression { get; init; } = default;
}
