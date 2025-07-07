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
    /// Gets the <see cref="GameDomain"/> for the <see cref="GameId"/>.
    /// </summary>
    GameDomain GetDomain(GameId id);

    /// <summary>
    /// Gets the <see cref="GameId"/> for the <see cref="GameDomain"/>.
    /// </summary>
    GameId GetId(GameDomain domain);

    /// <inheritdoc cref="GetDomain"/>
    GameDomain this[GameId id] => GetDomain(id);

    /// <inheritdoc cref="GetId"/>
    GameId this[GameDomain domain] => GetId(domain);
}
