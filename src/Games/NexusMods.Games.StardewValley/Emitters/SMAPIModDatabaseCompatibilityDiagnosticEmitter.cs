using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using ModStatus = StardewModdingAPI.Toolkit.Framework.ModData.ModStatus;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;
using SMAPIModDatabase = StardewModdingAPI.Toolkit.Framework.ModData.ModDatabase;

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
    private readonly ILogger<SMAPIModDatabaseCompatibilityDiagnosticEmitter> _logger;
    private readonly IFileStore _fileStore;

    public SMAPIModDatabaseCompatibilityDiagnosticEmitter(
        ILogger<SMAPIModDatabaseCompatibilityDiagnosticEmitter> logger,
        IFileStore fileStore)
    {
        _logger = logger;
        _fileStore = fileStore;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var optionalSMAPIMod = loadout.Mods
            .Where(kv => kv.Value.Enabled)
            .FirstOrOptional(kv => kv.Value.Metadata
                .OfType<SMAPIMarker>()
                .Any()
            );

        if (!optionalSMAPIMod.HasValue) yield break;
        var smapiMod = optionalSMAPIMod.Value.Value;

        var modDatabase = await GetModDatabase(smapiMod, cancellationToken);
        if (modDatabase is null) yield break;

        var smapiMods = loadout.Mods
            .Where(kv => kv.Value.Enabled)
            .Select(kv => kv.Value)
            .SelectAsync(async mod => (Mod: mod, Manifest: await GetManifest(mod, cancellationToken)))
            .Where(tuple => tuple.Manifest is not null);

        var asyncEnumerable = smapiMods.WithCancellation(cancellationToken).ConfigureAwait(false);
        await foreach (var tuple in asyncEnumerable)
        {
            var (mod, manifest) = tuple;
            var uniqueId = manifest!.UniqueID;

            var dataRecord = modDatabase.Get(uniqueId);
            if (dataRecord is null) continue;

            var versionedFields = dataRecord.GetVersionedFields(manifest);
            if (versionedFields.Status != ModStatus.Obsolete) continue;

            var upperVersion = versionedFields.StatusUpperVersion;
            var matches = upperVersion is null ||
                          manifest.Version.IsOlderThan(upperVersion) ||
                          manifest.Version.Equals(upperVersion);

            if (!matches) continue;

            yield return Diagnostics.CreateModCompatabilityObsolete(
                Mod: mod.ToReference(loadout),
                ModName: mod.Name,
                ReasonPhrase: versionedFields.StatusReasonPhrase ?? "the feature/fix has been integrated into SMAPI or Stardew Valley or has otherwise been made obsolete."
            );
        }
    }

    private async ValueTask<SMAPIModDatabase?> GetModDatabase(Mod smapi, CancellationToken cancellationToken)
    {
        try
        {
            return await Interop.GetModDatabase(_fileStore, smapi, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignored
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception trying to get mod database of SMAPI");
            return null;
        }
    }

    private async ValueTask<SMAPIManifest?> GetManifest(Mod mod, CancellationToken cancellationToken)
    {
        try
        {
            return await Interop.GetManifest(_fileStore, mod, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignored
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception trying to get manifest for mod {Mod}", mod.Name);
            return null;
        }
    }
}
