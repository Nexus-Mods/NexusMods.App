using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

namespace NexusMods.Networking.NexusWebApi.V1Interop;

internal class LocalMappingCache : IGameDomainToGameIdMappingCache
{
    private readonly FrozenDictionary<GameId, GameDomain> _gameIdToDomain;
    private readonly FrozenDictionary<GameDomain, GameId> _gameDomainToId;

    public LocalMappingCache(FrozenDictionary<GameId, GameDomain> gameIdToDomain, FrozenDictionary<GameDomain, GameId> gameDomainToId)
    {
        _gameIdToDomain = gameIdToDomain;
        _gameDomainToId = gameDomainToId;
    }

    public GameDomain GetDomain(GameId id) => _gameIdToDomain[id];
    public GameId GetId(GameDomain domain) => _gameDomainToId[domain];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        TypeInfoResolver = GameMetadataContext.Default,
    };

    internal static bool TryParseJsonFile([NotNullWhen(true)] out FrozenDictionary<GameId, GameDomain>? gameIdToDomain, [NotNullWhen(true)] out FrozenDictionary<GameDomain, GameId>? gameDomainToId)
    {
        gameIdToDomain = null;
        gameDomainToId = null;

        try
        {
            using var stream = typeof(LocalMappingCache).Assembly.GetManifestResourceStream(name: "games.json");
            if (stream is null) return false;

            var results = JsonSerializer.Deserialize<GameMetadata[]>(stream, SerializerOptions);
            if (results is null || results.Length == 0) return false;

            var pairs = results
                .Select(x => (Id: GameId.From(x.Id), Domain: GameDomain.From(x.DomainName)))
                .ToArray();

            gameIdToDomain = pairs.DistinctBy(tuple => tuple.Id).ToFrozenDictionary(tuple => tuple.Id, tuple => tuple.Domain);
            gameDomainToId = pairs.DistinctBy(tuple => tuple.Domain).ToFrozenDictionary(tuple => tuple.Domain, tuple => tuple.Id);
            return true;
        }
        catch (Exception e)
        {
            Debugger.Log(level: 4, message: $"Exception reading games.json file: {e}", category: null);
            return false;
        }
    }
}
