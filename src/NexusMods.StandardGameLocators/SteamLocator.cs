using GameFinder.StoreHandlers.Steam;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by 'Steam'.
/// </summary>
public class SteamLocator : AGameLocator<SteamGame, SteamGameId, ISteamGame, SteamLocator>
{
    /// <inheritdoc />
    public SteamLocator(IServiceProvider provider) : base(provider) { }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.Steam;

    /// <inheritdoc />
    protected override IEnumerable<SteamGameId> Ids(ISteamGame game) => game.SteamIds.Select(SteamGameId.From);

    /// <inheritdoc />
    protected override AbsolutePath Path(SteamGame record) => record.Path;

    /// <inheritdoc />
    protected override IGameLocatorResultMetadata CreateMetadata(SteamGame game)
    {
        return new SteamLocatorResultMetadata
        {
            AppId = game.AppId.Value,
            CloudSavesDirectory = game.CloudSavesDirectory
        };
    }
}
