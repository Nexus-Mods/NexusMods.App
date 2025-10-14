using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Games.CreationEngine.Fallout4;
using NexusMods.Games.IntegrationTestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.IntegrationTests.CreationEngine.Tests;

public class Fallout4SteamCurrentAttribute : SteamIntegrationTestAttribute<Fallout4>
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
