using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Emitters;

public class MissingSMAPIEmitter : ILoadoutDiagnosticEmitter
{
    private static readonly NamedLink NexusModsSMAPILink = new("Nexus Mods", new Uri("https://nexusmods.com/stardewvalley/mods/2400"));

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var smapiModCount = loadout.CountModsWithMetadata<SMAPIModMarker>();
        if (smapiModCount == 0) yield break;

        var optionalSmapiMod = loadout.GetFirstModWithMetadata<SMAPIMarker>(onlyEnabledMods: false);
        if (!optionalSmapiMod.HasValue)
        {
            yield return Diagnostics.CreateSMAPIRequiredButNotInstalled(
                ModCount: smapiModCount,
                NexusModsSMAPIUri: NexusModsSMAPILink
            );

            yield break;
        }

        var smapiMod = optionalSmapiMod.Value.Item1;
        if (!smapiMod.Enabled)
        {
            yield return Diagnostics.CreateSMAPIRequiredButDisabled(
                ModCount: smapiModCount
            );
        }
    }
}
