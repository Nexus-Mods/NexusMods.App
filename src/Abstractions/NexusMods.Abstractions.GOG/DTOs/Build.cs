using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.GOG.JsonConverters;
using NexusMods.Abstractions.GOG.Values;

namespace NexusMods.Abstractions.GOG.DTOs;

/// <summary>
/// Information about a build, which is a collection of files and the chunks that make up those files. This may sound confusing, but the idea
/// is that "build" and to some extent "depot" are both metadata concepts. The Build is the information about what a collection of files are tagged
/// as (like Cyberpunk 1.5) and the depot is metadata about the actual files that are stored on the CDN. There is a 1:1 relationship between depots and
/// builds. The files in the depot are then stored in one of many CDNs, these CDN links are known as "SecureLinks". There may be many secure links (mirrors)
/// for a given depot and build.
/// </summary>
[UsedImplicitly]
public class Build
{
    /// <summary>
    /// The unique ID of the build.
    /// </summary>
    [JsonPropertyName("build_id")]
    public required BuildId BuildId { get; init; }
    
    /// <summary>
    /// The product ID of the build.
    /// </summary>
    [JsonPropertyName("product_id")]
    public required ProductId ProductId { get; init; }
    
    /// <summary>
    /// The OS of the build.
    /// </summary>
    [JsonPropertyName("os")]
    public required string OS { get; init; }
    
    /// <summary>
    /// The version of the build.
    /// </summary>
    [JsonPropertyName("version_name")]
    public required string VersionName { get; init; }
    
    /// <summary>
    /// Various tags for the build (not sure what these may contain).
    /// </summary>
    [JsonPropertyName("tags")]
    public required string[] Tags { get; init; }
    
    /// <summary>
    /// True if the build is public, false if it is private.
    /// </summary>
    [JsonPropertyName("public")]
    public bool Public { get; init; }
    
    /// <summary>
    /// The date the build was published.
    /// </summary>
    [JsonConverter(typeof(GOGDateTimeOffsetConverter))]
    [JsonPropertyName("date_published")]
    public DateTimeOffset DatePublished { get; init; }
    
    /// <summary>
    /// The generation of the build data (should be 2).
    /// </summary>
    [JsonPropertyName("generation")]
    public int Generation { get; init; }
    
    /// <summary>
    /// A link to the build's details
    /// </summary>
    [JsonPropertyName("link")]
    public required Uri? Link { get; init; }
}
