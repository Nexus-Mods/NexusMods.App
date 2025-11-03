using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi.V1Interop;

internal class LocalMappingCache : IGameDomainToGameIdMappingCache
{
    private readonly ILogger _logger;
    private readonly FrozenDictionary<NexusModsGameId, GameDomain> _gameIdToDomain;
    private readonly FrozenDictionary<GameDomain, NexusModsGameId> _gameDomainToId;
    private readonly IGameDomainToGameIdMappingCache _fallbackCache;

    public LocalMappingCache(
        ILogger<LocalMappingCache> logger,
        FrozenDictionary<NexusModsGameId, GameDomain> gameIdToDomain,
        FrozenDictionary<GameDomain, NexusModsGameId> gameDomainToId,
        IGameDomainToGameIdMappingCache fallbackCache)
    {
        _logger = logger;
        _gameIdToDomain = gameIdToDomain;
        _gameDomainToId = gameDomainToId;
        _fallbackCache = fallbackCache;
    }

    public GameDomain GetDomain(NexusModsGameId id)
    {
        if (_gameIdToDomain.TryGetValue(id, out var domain)) return domain;
        _logger.LogDebug("Using fallback cache for game id {Id}", id);

        return _fallbackCache[id];
    }

    public NexusModsGameId GetId(GameDomain domain)
    {
        if (_gameDomainToId.TryGetValue(domain, out var id)) return id;
        _logger.LogDebug("Using fallback cache for game id {Domain}", domain);

        return _fallbackCache[domain];
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        TypeInfoResolver = GameMetadataContext.Default,
    };

    internal static bool TryParseJsonFile(
        ILogger logger,
        [NotNullWhen(true)] out FrozenDictionary<NexusModsGameId, GameDomain>? gameIdToDomain,
        [NotNullWhen(true)] out FrozenDictionary<GameDomain, NexusModsGameId>? gameDomainToId)
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
                .Select(x => (Id: NexusModsGameId.From(x.Id), Domain: GameDomain.From(x.DomainName)))
                .ToArray();

            gameIdToDomain = pairs.DistinctBy(tuple => tuple.Id).ToFrozenDictionary(tuple => tuple.Id, tuple => tuple.Domain);
            gameDomainToId = pairs.DistinctBy(tuple => tuple.Domain).ToFrozenDictionary(tuple => tuple.Domain, tuple => tuple.Id);
            Debug.Assert(gameIdToDomain.Count == gameDomainToId.Count);
            return true;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Exception reading games.json file");
            return false;
        }
    }
}
