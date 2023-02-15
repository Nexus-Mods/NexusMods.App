using System.Text.Json.Serialization;
using NexusMods.Networking.NexusWebApi.Types;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Networking.NexusWebApi.DTOs;

/// <summary>
/// Represents an individual download link returned from the API.
/// </summary>
/// <remarks>
///    At the current moment in time; only premium users can receive this; with the exception of NXM links.
/// </remarks>
public class DownloadLink
{
    /// <summary/>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Name of the CDN server that handles your download request.
    /// </summary>
    [JsonPropertyName("short_name")]
    public required CDNName ShortName { get; set; }
    
    /// <summary>
    /// Download URI used to download the files.
    /// </summary>
    [JsonPropertyName("URI")]
    public required Uri Uri { get; set; }
}