using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
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

    /// <inheritdoc />
    public ValueTask<Optional<GameId>> TryGetIdAsync(GameDomain gameDomain, CancellationToken cancellationToken)
    {
        // Check if we have a value in the DB
        var found = GameDomainToGameIdMapping.FindByDomain(_conn.Db, gameDomain);
        return found.TryGetFirst(out var mapping) ? ValueTask.FromResult<Optional<GameId>>(mapping.GameId) : ImplAsync();

        async ValueTask<Optional<GameId>> ImplAsync()
        {
            var gameId = await QueryIdFromDomainAsync(gameDomain, cancellationToken).ConfigureAwait(false);
            return !gameId.Equals(GameId.DefaultValue) ? gameId : Optional<GameId>.None;
        }
    }

    /// <inheritdoc />
    public ValueTask<Optional<GameDomain>> TryGetDomainAsync(GameId gameId, CancellationToken cancellationToken)
    {
        var found = GameDomainToGameIdMapping.FindByGameId(_conn.Db, gameId);
        return found.TryGetFirst(out var mapping) ? ValueTask.FromResult<Optional<GameDomain>>(mapping.Domain) : ImplAsync();

        async ValueTask<Optional<GameDomain>> ImplAsync()
        {
            var gameDomain = await QueryDomainFromIdAsync(gameId, cancellationToken).ConfigureAwait(false);
            return !gameDomain.Equals(GameDomain.DefaultValue) ? gameDomain : Optional<GameDomain>.None;
        }
    }

    /// <inheritdoc />
    public Optional<GameId> TryGetId(GameDomain gameDomain, CancellationToken cancellationToken)
    {
        // Check cache synchronously
        var found = GameDomainToGameIdMapping.FindByDomain(_conn.Db, gameDomain);
        if (found.TryGetFirst(out var mapping))
            return mapping.GameId;

        // Cache miss: Perform the async operation within Task.Run to avoid deadlocks
        try
        {
            return Task.Run(async () => await TryGetIdAsync(gameDomain, cancellationToken), cancellationToken)
                       .GetAwaiter()
                       .GetResult();
        }
        catch (AggregateException ae)
        {
            // Unwrap AggregateException if necessary
            throw ae.InnerException ?? ae;
        }
    }

    /// <inheritdoc />
    public Optional<GameDomain> TryGetDomain(GameId gameId, CancellationToken cancellationToken)
    {
        // Check cache synchronously
        var found = GameDomainToGameIdMapping.FindByGameId(_conn.Db, gameId);
        if (found.TryGetFirst(out var mapping))
            return mapping.Domain;

        // Cache miss: Perform the async operation within Task.Run to avoid deadlocks
        try
        {
            return Task.Run(async () => await TryGetDomainAsync(gameId, cancellationToken), cancellationToken)
                       .GetAwaiter()
                       .GetResult();
        }
        catch (AggregateException ae)
        {
            // Unwrap AggregateException if necessary
            throw ae.InnerException ?? ae;
        }
    }

    private async Task<GameId> QueryIdFromDomainAsync(GameDomain gameDomain, CancellationToken cancellationToken)
    {
        try
        {
            var game = await _gqlClient.GameDomainToId.ExecuteAsync(gameDomain.Value, cancellationToken).ConfigureAwait(false);
            game.EnsureNoErrors();
            if (game.Data?.Game == null)
            {
                _logger.LogError("Unable to find game with domain name {DomainName}", gameDomain.Value);
                return GameId.DefaultValue;
            }

            var id = GameId.From((uint)game.Data.Game.Id);
            await InsertAsync(gameDomain, id).ConfigureAwait(false);
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
            var game = await _gqlClient.GameIdToDomain.ExecuteAsync(gameId.ToString(), cancellationToken).ConfigureAwait(false);
            if (game.Data?.Game == null)
            {
                // ReSharper disable once InconsistentLogPropertyNaming
                _logger.LogError("Unable to find game with game ID {GameID}", gameId);
                return GameDomain.DefaultValue;
            }

            var domain = GameDomain.From(game.Data.Game.DomainName);
            await InsertAsync(domain, gameId).ConfigureAwait(false);
            return domain;
        }
        catch (Exception e)
        {
            // ReSharper disable once InconsistentLogPropertyNaming
            _logger.LogError(e, "Exception while querying domain from ID {GameID}", gameId);
            return GameDomain.DefaultValue;
        }
    }

    /// <inheritdoc/>
    public async ValueTask InsertAsync(GameDomain gameDomain, GameId gameId)
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
        await tx.Commit().ConfigureAwait(false);
    }
}
