using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using GameFinder.StoreHandlers.Steam.Services;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
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
    protected override IGameLocatorResultMetadata CreateMetadata(SteamGame game, IEnumerable<SteamGame> otherFoundGames)
    {
        var winePrefixDirectoryPath = game.GetProtonPrefix()?.ProtonDirectory.Combine("pfx");
        var linuxCompatibilityDataProvider = winePrefixDirectoryPath is not null ? new LinuxCompatibilityDataProvider(game, winePrefixDirectoryPath.Value) : null;

        return new SteamLocatorResultMetadata
        {
            AppId = game.AppId.Value,
            ManifestIds = game.AppManifest.InstalledDepots.Select(x => x.Value.ManifestId.Value).ToArray(),
            CloudSavesDirectory = game.GetCloudSavesDirectoryPath(),
            LinuxCompatibilityDataProvider = linuxCompatibilityDataProvider,
        };
    }

    private class LinuxCompatibilityDataProvider : BaseLinuxCompatibilityDataProvider
    {
        private readonly SteamGame _steamGame;

        public LinuxCompatibilityDataProvider(SteamGame steamGame, AbsolutePath winePrefixDirectoryPath) : base(winePrefixDirectoryPath)
        {
            _steamGame = steamGame;
        }

        public override ValueTask<WineDllOverride[]> GetWineDllOverrides(CancellationToken cancellationToken)
        {
            var localConfigPath = SteamLocationFinder.GetUserDataDirectoryPath(_steamGame.SteamPath, _steamGame.AppManifest.LastOwner).Combine("config").Combine("localconfig.vdf");
            var parserResult = LocalUserConfigParser.ParseConfigFile(_steamGame.AppManifest.LastOwner, localConfigPath);
            if (parserResult.IsFailed || !parserResult.Value.LocalAppData.TryGetValue(_steamGame.AppId, out var localAppData)) return ValueTask.FromResult<WineDllOverride[]>([]);

            var launchOptions = localAppData.LaunchOptions;
            var section = WineParser.GetWineDllOverridesSection(launchOptions);
            var result = WineParser.ParseEnvironmentVariable(section);
            return ValueTask.FromResult(result.ToArray());
        }
    }
}
