using NexusMods.Games.IntegrationTestFramework;

namespace NexusMods.Games.CreationEngine.Tests.TestAttributes;

/// <summary>
/// Integration test attribute for the current version of Fallout 4 on Steam.
/// </summary>
public class Fallout4SteamCurrentAttribute : SteamIntegrationTestAttribute<CreationEngine.Fallout4.Fallout4>
{
    protected override IEnumerable<(string Name, uint AppId, ulong[] ManifestIds)> GetManifestIds()
    {
        yield return ("Fallout 4", 377160, [
            7332110922360867314,
            5698952341602575696,
            8681102885670959037,
            8492427313392140315,
            1213339795579796878,
            7785009542965564688,
            366079256218893805,
            1207717296920736193,
            8482181819175811242,
            5527412439359349504,
            6588493486198824788,
            5000262035721758737,
            4873048792354485093,
            7677765994120765493,
        ]);
    }
}
