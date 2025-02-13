using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Resources;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.WebAPI;
using NexusMods.Paths;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.ModData;
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
    private readonly ILogger _logger;
    private readonly IFileStore _fileStore;
    private readonly IOSInformation _os;
    private readonly ISMAPIWebApi _smapiWebApi;
    private readonly IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest> _manifestPipeline;

    private static readonly NamedLink DefaultWikiLink = new("SMAPI Wiki", new Uri("https://smapi.io/mods"));

    public SMAPIModDatabaseCompatibilityDiagnosticEmitter(
        IServiceProvider serviceProvider,
        ILogger<SMAPIModDatabaseCompatibilityDiagnosticEmitter> logger,
        IFileStore fileStore,
        IOSInformation os,
        ISMAPIWebApi smapiWebApi)
    {
        _logger = logger;
        _fileStore = fileStore;
        _os = os;
        _smapiWebApi = smapiWebApi;
        _manifestPipeline = Pipelines.GetManifestPipeline(serviceProvider);
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var gameVersion = new SemanticVersion((loadout.InstallationInstance.Game as AGame)!.GetLocalVersion(loadout.Installation));

        if (!Helpers.TryGetSMAPI(loadout, out var smapi)) yield break;
        if (!SMAPILoadoutItem.Version.TryGetValue(smapi, out var smapiStrVersion)) yield break;
        if (!SemanticVersion.TryParse(smapiStrVersion, out var smapiVersion))
        {
            _logger.LogError("Unable to parse `{Version}` as a semantic version", smapiStrVersion);
            yield break;
        }

        var modDatabase = await GetModDatabase(smapi, cancellationToken);
        if (modDatabase is null) yield break;

        var smapiMods = await Helpers
            .GetAllManifestsAsync(_logger, loadout, _manifestPipeline, onlyEnabledMods: true, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var list = new List<(SMAPIModLoadoutItem.ReadOnly smapiMod, SMAPIManifest manifest, ModDataRecordVersionedFields versionedFields)>();

        foreach (var tuple in smapiMods)
        {
            var (smapiMod, manifest) = tuple;
            var uniqueId = manifest.UniqueID;

            var dataRecord = modDatabase.Get(uniqueId);
            if (dataRecord is null) continue;

            var versionedFields = dataRecord.GetVersionedFields(manifest);
            if (versionedFields.Status is not ModStatus.Obsolete and not ModStatus.AssumeBroken) continue;

            var upperVersion = versionedFields.StatusUpperVersion;
            var matches = upperVersion is null ||
                          manifest.Version.IsOlderThan(upperVersion) ||
                          manifest.Version.Equals(upperVersion);

            if (!matches) continue;

            list.Add((smapiMod, manifest, versionedFields));
        }

        var apiMods = await _smapiWebApi.GetModDetails(
            os: _os,
            gameVersion,
            smapiVersion,
            smapiIDs: list.Select(tuple => tuple.Item2.UniqueID).ToArray()
        );

        foreach (var tuple in list)
        {
            var (smapiMod, manifest, versionedFields) = tuple;
            var reasonPhrase = versionedFields.StatusReasonPhrase ?? versionedFields.StatusReasonDetails;

            if (versionedFields.Status == ModStatus.Obsolete)
            {
                yield return Diagnostics.CreateModCompatabilityObsolete(
                    SMAPIMod: smapiMod.AsLoadoutItemGroup().ToReference(loadout),
                    SMAPIModName: smapiMod.AsLoadoutItemGroup().AsLoadoutItem().Name,
                    ReasonPhrase: reasonPhrase ?? "the feature/fix has been integrated into SMAPI or Stardew Valley or has otherwise been made obsolete."
                );
            } else if (versionedFields.Status == ModStatus.AssumeBroken)
            {
                yield return Diagnostics.CreateModCompatabilityAssumeBroken(
                    SMAPIMod: smapiMod.AsLoadoutItemGroup().ToReference(loadout),
                    SMAPIModName: smapiMod.AsLoadoutItemGroup().AsLoadoutItem().Name,
                    ReasonPhrase: reasonPhrase ?? "it's no longer compatible",
                    ModLink:apiMods.GetLink(manifest.UniqueID, defaultValue: DefaultWikiLink),
                    ModVersion: manifest.Version.ToString()
                );
            }
        }
    }

    private async ValueTask<SMAPIModDatabase?> GetModDatabase(SMAPILoadoutItem.ReadOnly smapi, CancellationToken cancellationToken)
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
}
