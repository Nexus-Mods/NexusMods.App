using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Stores.Steam;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by 'Steam'.
/// </summary>
public class SteamLocator : AGameLocator<SteamGame, AppId, ISteamGame, SteamLocator>
{
    /// <inheritdoc />
    public SteamLocator(IServiceProvider provider) : base(provider) { }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.Steam;

    /// <inheritdoc />
    protected override IEnumerable<AppId> Ids(ISteamGame game) => game.SteamIds.Select(AppId.From);

    /// <inheritdoc />
    protected override AbsolutePath Path(SteamGame record) => record.Path;

    /// <inheritdoc />
    protected override IGameLocatorResultMetadata CreateMetadata(SteamGame game)
    {
        return new SteamLocatorResultMetadata
        {
            AppId = game.AppId.Value,
            CloudSavesDirectory = game.GetCloudSavesDirectoryPath(),
        };
    }
}
