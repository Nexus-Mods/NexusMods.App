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

        var hasSMAPI = loadout.Mods.Any(kv => kv.Value.Metadata.OfType<SMAPIMarker>().Any());
        if (hasSMAPI) yield break;

        yield return Diagnostics.CreateMissingSMAPI(
            ModCount: smapiModCount,
            NexusModsSMAPIUri: NexusModsSMAPIUri
        );
    }
}
