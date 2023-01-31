using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.DTOs;

public class ModUpdate
{
    [JsonPropertyName("mod_id")]
    public ModId ModId { get; set; }
    
    [JsonPropertyName("LatestFileUpdated")]
    public DateTime LatestFileUpdated { get; set; }
    
    [JsonPropertyName("LatestModActivity")]
    public DateTime LatestModActivity { get; set; }
}