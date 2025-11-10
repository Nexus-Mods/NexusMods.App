using NexusMods.Sdk.NexusModsApi;
using GameDomain = NexusMods.Abstractions.NexusWebApi.Types.GameDomain;

namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// Caches the mapping between <see cref="GameDomain"/> and <see cref="NexusModsGameId"/> values for fast lookup.
/// Queries the API to populate missing values.
/// </summary>
public interface IGameDomainToGameIdMappingCache
{
    /// <summary>
    /// Gets the <see cref="GameDomain"/> for the <see cref="NexusModsGameId"/>.
    /// </summary>
    GameDomain GetDomain(NexusModsGameId id);

    /// <summary>
    /// Gets the <see cref="NexusModsGameId"/> for the <see cref="GameDomain"/>.
    /// </summary>
    NexusModsGameId GetId(GameDomain domain);

    /// <inheritdoc cref="GetDomain"/>
    GameDomain this[NexusModsGameId id] => GetDomain(id);

    /// <inheritdoc cref="GetId"/>
    NexusModsGameId this[GameDomain domain] => GetId(domain);
}
