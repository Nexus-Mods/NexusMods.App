using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Games.StardewValley.Emitters;

/// <summary>
/// This diagnostic emitter uses the internal SMAPI "mod database" to create
/// compatibility diagnostics. The internal "mod database" from SMAPI can
/// be found in the "smapi-internal/metadata.json" file or on GitHub:
///
/// https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI.Web/wwwroot/SMAPI.metadata.json
///
/// NOTE(erri120): Technically, we can always get the latest version using the
/// link above, or through the SMAPI server: https://smapi.io/SMAPI.metadata.json.
///
/// However, these compatibility details only make sense for the currently installed
/// SMAPI version, so we'll use the local metadata file instead of the remote one.
/// </summary>
public class SMAPIModDatabaseCompatibilityDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    public SMAPIModDatabaseCompatibilityDiagnosticEmitter()
    {
        
    }

    public IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
