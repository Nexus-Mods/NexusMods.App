using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;

namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Download links for a collection.
/// </summary>
public class CollectionDownloadLinks : IJsonSerializable<CollectionDownloadLinks>
{
    [JsonPropertyName("download_links")]
    public required CollectionLink[] DownloadLinks { get; set; }
    
    /// <inheritdoc />
    public static JsonTypeInfo<CollectionDownloadLinks> GetTypeInfo() => CollectionDownloadLinksContext.Default.CollectionDownloadLinks;
}

/// <summary>
/// A single download link.
/// </summary>
public class CollectionLink
{
    /// <summary>
    /// The CDN name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// The CDN short name
    /// </summary>
    [JsonPropertyName("short_name")]
    public required string ShortName { get; set; }
    
    /// <summary>
    /// URI to download the file
    /// </summary>
    [JsonPropertyName("URI")]
    public required Uri Uri { get; set; }
}


// Note for future readers: JsonSourceGenerationMode.Serialization is for Serialization only;
// this code will be redundant for us as we deserialize only; hence we don't generate it.
/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(CollectionDownloadLinks))]
public partial class CollectionDownloadLinksContext : JsonSerializerContext { }
