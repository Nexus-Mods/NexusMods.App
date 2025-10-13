using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Games.CreationEngine.SkyrimSE;
using NexusMods.Games.IntegrationTestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.IntegrationTests.CreationEngine.Tests;

public abstract class ASkyrimSETest : AGameIntegrationTest<SkyrimSE>
{
    protected override IEnumerable<GameLocatorResult> Locators
    {
        get
        {
            var steamFolder = FileSystem.GetKnownPath(KnownPath.ProgramFilesDirectory) / "Steam" / "steamapps" / "common" / "Skyrim Special Edition";
            yield return new GameLocatorResult(steamFolder, FileSystem, OSInformation.FakeWindows,
                GameStore.Steam, new SteamLocatorResultMetadata
                {
                    AppId = 489830,
                    ManifestIds = [1914580699073641964, 8042843504692938467, 8442952117333549665],
                }
            );
        }
    }
}
