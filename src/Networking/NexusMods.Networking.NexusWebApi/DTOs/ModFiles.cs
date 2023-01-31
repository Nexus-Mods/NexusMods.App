using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi.DTOs;

public class ModFiles
{
    [JsonPropertyName("files")]
    public ModFile[] Files { get; set; }
}
