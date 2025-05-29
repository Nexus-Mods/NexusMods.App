using System.Text.Json.Serialization;
using NexusMods.Abstractions.GOG.Values;

namespace NexusMods.Networking.GOG.DTOs;

/// <summary>
/// Response from /user/data/games
/// </summary>
public class OwnedGamesList
{
    [JsonPropertyName("owned")]
    public List<ProductId> Owned { get; set; } = [];
}
