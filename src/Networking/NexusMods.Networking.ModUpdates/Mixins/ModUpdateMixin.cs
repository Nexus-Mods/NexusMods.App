using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Networking.ModUpdates.Traits;
namespace NexusMods.Networking.ModUpdates.Mixins;

/// <summary>
/// Implements the (V1) mod update API mixin.
/// </summary>
public struct ModUpdateMixin : ICanGetUidForMod, ICanGetLastUpdatedTimestamp
{
    private readonly DateTime _lastUpdatedDate;
    private readonly GameId _gameId;
    private readonly ModId _modId;

    /// <summary/>
    public ModUpdateMixin(ModUpdate update, GameId gameId)
    {
        _lastUpdatedDate = update.LatestModActivityUtc;
        _gameId = gameId;
        _modId = update.ModId;
    }

    /// <summary>
    /// Transforms the result of a V1 API call for mod updates into the Mixin.
    /// </summary>
    public static IEnumerable<ModUpdateMixin> FromUpdateResults(IEnumerable<ModUpdate> updates, GameId gameId) => updates.Select(update => new ModUpdateMixin(update, gameId));

    /// <inheritdoc />
    public DateTime GetLastUpdatedDate() => _lastUpdatedDate;

    /// <inheritdoc />
    public UidForMod GetUniqueId() => new()
    {
        GameId = _gameId,
        ModId = _modId, 
    };
}
