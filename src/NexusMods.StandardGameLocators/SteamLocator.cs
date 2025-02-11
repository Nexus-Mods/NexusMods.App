using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
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
    protected override AbsolutePath Path(SteamGame game)
    {
        return game.Path;
    }

    /// <inheritdoc />
    protected override IFileSystem GetMappedFileSystem(SteamGame game)
    {
        if (!OSInformation.Shared.IsLinux) return base.GetMappedFileSystem(game);

        var protonPrefix = game.GetProtonPrefix();
        if (protonPrefix is null) return base.GetMappedFileSystem(game);

        return protonPrefix.CreateOverlayFileSystem(FileSystem.Shared);
    }

    /// <inheritdoc />
    protected override IGameLocatorResultMetadata CreateMetadata(SteamGame game)
    {
        return new SteamLocatorResultMetadata
        {
            AppId = game.AppId.Value,
            ManifestIds = game.AppManifest.InstalledDepots.Select(x => x.Value.ManifestId.Value).ToArray(),
            CloudSavesDirectory = game.GetCloudSavesDirectoryPath(),
        };
    }
}
