using NexusMods.Games.IntegrationTestFramework;

namespace NexusMods.Games.CreationEngine.Tests.TestAttributes;

/// <summary>
/// Integration test attribute for the current version of Skyrim Special Edition on Steam.
/// </summary>
public class SkyrimSESteamCurrentAttribute : SteamIntegrationTestAttribute<CreationEngine.SkyrimSE.SkyrimSE>
{
    protected override IEnumerable<(string Name, uint AppId, ulong[] ManifestIds)> GetManifestIds()
    {
        yield return ("Skyrim Special Edition", 489830, [1914580699073641964, 8042843504692938467, 8442952117333549665]);
    }
}
