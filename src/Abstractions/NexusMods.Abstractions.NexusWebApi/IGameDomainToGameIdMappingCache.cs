using DynamicData.Kernel;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using GameDomain = NexusMods.Abstractions.NexusWebApi.Types.GameDomain;
namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// Caches the mapping between <see cref="GameDomain"/> and <see cref="GameId"/> values for fast lookup.
/// Queries the API to populate missing values.
/// </summary>
public interface IGameDomainToGameIdMappingCache
{
    /// <summary>
    /// Tries to get the corresponding <see cref="GameId"/>.
    /// If there is no mapping, the API will be queried to get the mapping.
    /// </summary>
    ValueTask<Optional<GameId>> TryGetIdAsync(GameDomain gameDomain, CancellationToken cancellationToken);

    /// <summary>
    /// Synchronously tries to get the corresponding <see cref="GameId"/>.
    /// If there is no mapping, the API will be queried to get the mapping on the ThreadPool.
    /// </summary>
    /// <remarks>
    ///     Underlying code is still asynchronous, but will always result in a synchronous
    ///     cache hit unless the translation is being done for the first time ever.
    /// </remarks>
    Optional<GameId> TryGetId(GameDomain gameDomain, CancellationToken cancellationToken);
    
    /// <summary>
    /// Tries to get the corresponding <see cref="GameDomain"/>.
    /// If there is no mapping, the API will be queried to get the mapping.
    /// </summary>
    ValueTask<Optional<GameDomain>> TryGetDomainAsync(GameId gameId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Synchronously tries to get the corresponding <see cref="GameDomain"/>.
    /// If there is no mapping, the API will be queried to get the mapping on the ThreadPool.
    /// </summary>
    /// <remarks>
    ///     Underlying code is still asynchronous, but will always result in a synchronous
    ///     cache hit unless the translation is being done for the first time ever.
    /// </remarks>
    Optional<GameDomain> TryGetDomain(GameId gameId, CancellationToken cancellationToken);

    /// <summary>
    /// Puts the values into the cache.
    /// </summary>
    ValueTask InsertAsync(GameDomain gameDomain, GameId gameId);
}
