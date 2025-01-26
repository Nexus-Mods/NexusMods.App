using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes.DTOs;

/// <summary>
/// The contents of a manifest.json file for a release of the hashes DB. We use
/// this instead of Github's APIs because those are more heavily rate-limited.
/// </summary>
public class Manifest
{
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    [JsonPropertyName("assets")]
    public List<Asset> Assets { get; set; } = [];
}

public class Asset
{
    [JsonPropertyName("type")]
    public AssetType Type { get; set; }
    
    [JsonPropertyName("hash")]
    public Hash Hash { get; set; }
    
    [JsonPropertyName("size")]
    public Size Size { get; set; }
}

public enum AssetType
{
    [JsonStringEnumMemberName("game_hash_db")]
    GameHashDb,
}
