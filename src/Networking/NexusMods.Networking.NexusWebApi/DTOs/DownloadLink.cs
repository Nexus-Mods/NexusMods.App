using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.DTOs;

public class DownloadLink
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("short_name")]
    public required CDNName ShortName { get; set; }
    
    [JsonPropertyName("URI")]
    public required Uri Uri { get; set; }
}