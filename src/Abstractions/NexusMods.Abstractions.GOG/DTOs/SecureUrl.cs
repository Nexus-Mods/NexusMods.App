using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.GOG.JsonConverters;
// ReSharper disable InconsistentNaming

namespace NexusMods.Abstractions.GOG.DTOs;

/// <summary>
/// A secure URL for a GOG endpoint. These are tempoarary URLs, that are combined with a given
/// MD5 hash to get a chunk of data for a depot.
/// </summary>
[UsedImplicitly]
public class SecureUrl
{
    /// <summary>
    /// The name of the endpoint this secure URL is for.
    /// </summary>
    [JsonPropertyName("endpoint_name")]
    public required string EndpointName { get; init; }
     
    /// <summary>
    /// A string format template for the URL.
    /// </summary>
    [JsonPropertyName("url_format")]
    public required string UrlFormat { get; init; }
    
    /// <summary>
    /// The parameters for the URL format template.
    /// </summary>
    [JsonPropertyName("parameters")]
    public required SecureUrlParameters Parameters { get; init; }
    
    /// <summary>
    /// The priority of this secure URL, higher is better.
    /// </summary>
    [JsonPropertyName("priority")]
    public required int Priority { get; init; }
    
    /// <summary>
    /// Maximum number of failures before this secure URL is considered failed.
    /// </summary>
    [JsonPropertyName("max_fails")]
    public required int MaxFails { get; init; }
    
    /// <summary>
    /// Supported api generation types.
    /// </summary>
    [JsonPropertyName("supports_generation")]
    public required int[] SupportsGeneration { get; init; }
    
    /// <summary>
    /// True if this secure URL is a fallback only, not for primary use.
    /// </summary>
    [JsonPropertyName("fallback_only")]
    public required bool FallbackOnly { get; init; }
}

/// <summary>
/// Parameters for the secure URL format template. Some of the members of this class use JS style naming conventions
/// because they are part of the URL pattern and cannot be changed.
/// </summary>
[UsedImplicitly]
public record SecureUrlParameters
{
    
    /// <summary>
    /// The base URL for the secure URL.
    /// </summary>
    public required string base_url { get; init; }
    
    /// <summary>
    /// The path for the secure URL.
    /// </summary>
    public required string path { get; init; }
    
    /// <summary>
    /// Token value for the secure URL.
    /// </summary>
    [JsonPropertyName("token")]
    public required string token { get; init; }

    /// <summary>
    /// Expiry date for the secure URL. (optional)
    /// </summary>
    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(UnixToDateTimeOffsetConverter))]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Number of directories for the secure URL. (optional)
    /// </summary>
    public int? dirs { get; init; }
}
