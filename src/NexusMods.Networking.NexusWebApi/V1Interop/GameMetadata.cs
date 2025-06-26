using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi.V1Interop;

// https://data.nexusmods.com/file/nexus-data/games.json
internal record GameMetadata(
    [property: JsonPropertyName("id")] uint Id,
    [property: JsonPropertyName("domain_name")] string DomainName
);

[JsonSerializable(typeof(GameMetadata))]
[JsonSerializable(typeof(GameMetadata[]))]
internal partial class GameMetadataContext : JsonSerializerContext { }
