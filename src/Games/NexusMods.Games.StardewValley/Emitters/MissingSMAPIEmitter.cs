using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Emitters;

public class MissingSMAPIEmitter : ILoadoutDiagnosticEmitter
{
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var smapiModCount = loadout.CountModsWithMetadata(SMAPIModMarker.IsSMAPIMod);
        if (smapiModCount == 0) yield break;

        var optionalSmapiMod = loadout.GetFirstModWithMetadata(SMAPIMarker.Version, onlyEnabledMods: false);
        if (!optionalSmapiMod.HasValue)
        {
            yield return Diagnostics.CreateSMAPIRequiredButNotInstalled(
                ModCount: smapiModCount,
                NexusModsSMAPIUri: Helpers.SMAPILink
            );

            yield break;
        }

        if (!loadout.GetFirstModWithMetadata(SMAPIMarker.Version, onlyEnabledMods: true).HasValue)
        {
            yield return Diagnostics.CreateSMAPIRequiredButDisabled(
                ModCount: smapiModCount
            );
        }
    }
}
