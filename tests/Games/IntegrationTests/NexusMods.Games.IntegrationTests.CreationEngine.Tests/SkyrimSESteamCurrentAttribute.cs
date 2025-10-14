using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Games.CreationEngine.SkyrimSE;
using NexusMods.Games.IntegrationTestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.IntegrationTests.CreationEngine.Tests;

public class SkyrimSESteamCurrentAttribute : SteamIntegrationTestAttribute<SkyrimSE>
{
    protected override IEnumerable<(string Name, uint AppId, ulong[] ManifestIds)> GetManifestIds()
    {
        yield return ("Skyrim Special Edition", 489830, [1914580699073641964, 8042843504692938467, 8442952117333549665]);
    }
}
