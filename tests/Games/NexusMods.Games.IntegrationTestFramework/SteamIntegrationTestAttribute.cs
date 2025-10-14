using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Games.CreationEngine.SkyrimSE;
using NexusMods.Paths;

namespace NexusMods.Games.IntegrationTestFramework;

public abstract class SteamIntegrationTestAttribute<TGame> : LocatorResultAttribute<TGame> 
    where TGame : IGame
{
    protected abstract IEnumerable<(string Name, uint AppId, ulong[] ManifestIds)> GetManifestIds();
    protected override IEnumerable<GameLocatorResult> GetLocatorResults()
    {
        foreach (var (name, appId, manifestIds) in GetManifestIds())
        {
            var fileSystem = new InMemoryFileSystem();

            AbsolutePath steamBase;
            if (OSInformation.Shared.IsWindows)
                steamBase = fileSystem.GetKnownPath(KnownPath.ProgramFilesDirectory) / "Steam";
            else if (OSInformation.Shared.IsLinux)
                steamBase = fileSystem.GetKnownPath(KnownPath.HomeDirectory) / ".steam";
            else
                throw new NotImplementedException("Add OS mappings for this OS");

            var steamFolder = steamBase / "steamapps" / "common" / name;
            yield return new GameLocatorResult(steamFolder, fileSystem, OSInformation.Shared,
                GameStore.Steam, new SteamLocatorResultMetadata
                {
                    AppId = appId,
                    ManifestIds = manifestIds,
                }
            );
        }
    }
}
