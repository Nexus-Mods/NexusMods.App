using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Emitters;

public class MissingSMAPIEmitter : ILoadoutDiagnosticEmitter
{
    private static readonly Uri NexusModsSMAPIUri = new("https://nexusmods.com/stardewvalley/mods/2400");

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        await Task.Yield();

        var smapiModCount = loadout.Mods.Count(kv => kv.Value.Metadata.OfType<SMAPIModMarker>().Any());
        if (smapiModCount == 0) yield break;

        var smapiInstallations = loadout.Mods
            .Where(kv => kv.Value.Metadata.OfType<SMAPIMarker>().Any())
            .ToArray();

        var hasSMAPI = smapiInstallations.Length == 0;
        if (!hasSMAPI)
        {
            yield return Diagnostics.CreateSMAPIRequiredButNotInstalled(
                ModCount: smapiModCount,
                NexusModsSMAPIUri: NexusModsSMAPIUri
            );
        }

        var hasSMAPIEnabled = smapiInstallations.Any(kv => kv.Value.Enabled);
        if (!hasSMAPIEnabled)
        {
            yield return Diagnostics.CreateSMAPIRequiredButDisabled(
                ModCount: smapiModCount
            );
        }
    }
}
