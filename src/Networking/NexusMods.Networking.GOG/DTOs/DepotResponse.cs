using System.Text.Json.Serialization;
using NexusMods.Abstractions.GOG.DTOs;

namespace NexusMods.Networking.GOG.DTOs;

internal class DepotResponse
{
    [JsonPropertyName("depot")]
    public required DepotInfo Depot { get; init; }
        
    [JsonPropertyName("version")]
    public required int Version { get; init; }
}
