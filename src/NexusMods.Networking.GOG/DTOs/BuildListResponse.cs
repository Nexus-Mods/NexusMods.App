using System.Text.Json.Serialization;
using NexusMods.Abstractions.GOG.DTOs;

namespace NexusMods.Networking.GOG.DTOs;

internal class BuildListResponse
{
    [JsonPropertyName("items")]
    public required Build[] Items { get; init; }
}
