using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
namespace NexusMods.Networking.ModUpdates.Mixins;

/// <summary>
/// Implements the (V1) mod update API mixin.
/// </summary>
public readonly struct ModFeedItemUpdateMixin : IModFeedItem
{
    private readonly DateTimeOffset _lastUpdatedDate;
    private readonly GameId _gameId;
    private readonly ModId _modId;

    /// <summary/>
    private ModFeedItemUpdateMixin(ModUpdate update, GameId gameId)
    {
        // Note(sewer): V2 doesn't have 'last file updated' field, so we have to use 'last mod page update' time.
        // Well, this whole struct is, will be making that ticket to backend, and replace
        // this when V2 gets relevant API.
        _lastUpdatedDate = update.LatestModActivityUtc;
        _gameId = gameId;
        _modId = update.ModId;
    }

    /// <summary>
    /// Transforms the result of a V1 API call for mod updates into the Mixin.
    /// </summary>
    public static IEnumerable<ModFeedItemUpdateMixin> FromUpdateResults(IEnumerable<ModUpdate> updates, GameId gameId) => updates.Select(update => new ModFeedItemUpdateMixin(update, gameId));

    /// <inheritdoc />
    public DateTimeOffset GetLastUpdatedDate() => _lastUpdatedDate;

    /// <inheritdoc />
    public UidForMod GetModPageId() => new()
    {
        GameId = _gameId,
        ModId = _modId, 
    };
}
