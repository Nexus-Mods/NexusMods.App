using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Emitters;

public class MissingSMAPIEmitter : ILoadoutDiagnosticEmitter
{
    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var numEnabledSMAPIManifests = SMAPIManifestLoadoutFile.GetAllInLoadout(loadout.Db, loadout, onlyEnabled: true).Count();
        if (numEnabledSMAPIManifests == 0) yield break;

        var smapiLoadoutItems = loadout.Items.OfTypeLoadoutItemGroup().OfTypeSMAPILoadoutItem().ToArray();
        if (smapiLoadoutItems.Length == 0)
        {
            yield return Diagnostics.CreateSMAPIRequiredButNotInstalled(
                ModCount: numEnabledSMAPIManifests,
                NexusModsSMAPIUri: Helpers.SMAPILink
            );

            yield break;
        }

        var isSMAPIEnabled = smapiLoadoutItems.Any(x => x.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled());
        if (isSMAPIEnabled) yield break;

        yield return Diagnostics.CreateSMAPIRequiredButDisabled(
            ModCount: numEnabledSMAPIManifests
        );
    }
}
