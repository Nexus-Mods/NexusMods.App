using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using StrawberryShake;
namespace NexusMods.Networking.NexusWebApi.V1Interop;

/// <summary>
/// Caches the mapping between <see cref="GameDomain"/> and <see cref="GameId"/> values for fast lookup.
/// Queries the API to populate missing values.
/// </summary>
public sealed class GameDomainToGameIdMappingCache : IGameDomainToGameIdMappingCache
{
    private readonly IConnection _conn;
    private readonly INexusGraphQLClient _gqlClient;
    private readonly ILogger _logger;

    /// <summary/>
    public GameDomainToGameIdMappingCache(
        IConnection conn,
        INexusGraphQLClient gqlClient,
        ILogger<GameDomainToGameIdMappingCache> logger)
    {
        _conn = conn;
        _gqlClient = gqlClient;
        _logger = logger;
    }

    /// <summary>
    /// Tries to get the corresponding <see cref="GameId"/>.
    /// If there is no mapping, the API will be queried to get the mapping.
    /// </summary>
    public async ValueTask<Optional<GameId>> TryGetIdAsync(GameDomain gameDomain, CancellationToken cancellationToken)
    {
        // Check if we have a value in the DB
        var found = GameDomainToGameIdMapping.FindByDomain(_conn.Db, gameDomain);
        if (found.TryGetFirst(out var mapping))
            return mapping.GameId;

        // If we don't have a value, query the API for it, and then return the value from the DB
        var gameId = await QueryIdFromDomainAsync(gameDomain, cancellationToken);
        return !gameId.Equals(GameId.DefaultValue) ? gameId : Optional<GameId>.None;

    }

    /// <summary>
    /// Tries to get the corresponding <see cref="GameDomain"/>.
    /// If there is no mapping, the API will be queried to get the mapping.
    /// </summary>
    public async ValueTask<Optional<GameDomain>> TryGetDomainAsync(GameId gameId, CancellationToken cancellationToken)
    {
        var found = GameDomainToGameIdMapping.FindByGameId(_conn.Db, gameId);
        if (found.TryGetFirst(out var mapping))
            return mapping.Domain;

        var gameDomain = await QueryDomainFromIdAsync(gameId, cancellationToken);
        return !gameDomain.Equals(GameDomain.DefaultValue) ? gameDomain : Optional<GameDomain>.None;
    }

    private async Task<GameId> QueryIdFromDomainAsync(GameDomain gameDomain, CancellationToken cancellationToken)
    {
        try
        {
            var game = await _gqlClient.GameDomainToId.ExecuteAsync(gameDomain.Value, cancellationToken);
            game.EnsureNoErrors();
            if (game.Data?.Game == null)
            {
                _logger.LogError("Unable to find game with domain name {DomainName}", gameDomain.Value);
                return GameId.DefaultValue;
            }

            var id = GameId.From((uint)game.Data.Game.Id);
            await InsertAsync(gameDomain, id);
            return id;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while querying ID from domain {DomainName}", gameDomain.Value);
            return GameId.DefaultValue;
        }
    }

    private async Task<GameDomain> QueryDomainFromIdAsync(GameId gameId, CancellationToken cancellationToken)
    {
        try
        {
            var game = await _gqlClient.GameIdToDomain.ExecuteAsync(gameId.ToString(), cancellationToken);
            if (game.Data?.Game == null)
            {
                // ReSharper disable once InconsistentLogPropertyNaming
                _logger.LogError("Unable to find game with game ID {GameID}", gameId);
                return GameDomain.DefaultValue;
            }

            var domain = GameDomain.From(game.Data.Game.DomainName);
            await InsertAsync(domain, gameId);
            return domain;
        }
        catch (Exception e)
        {
            // ReSharper disable once InconsistentLogPropertyNaming
            _logger.LogError(e, "Exception while querying domain from ID {GameID}", gameId);
            return GameDomain.DefaultValue;
        }
    }

    private async ValueTask InsertAsync(GameDomain gameDomain, GameId gameId)
    {
        // Note(sewer): In theory, there's a race condition in here if multiple threads
        //              try to insert at once. However that should not be a concern here,
        //              there are no negative side effects.
        using var tx = _conn.BeginTransaction();
        _ = new GameDomainToGameIdMapping.New(tx)
        {
            Domain = gameDomain,
            GameId = gameId,
        };
        await tx.Commit();
    }
}
