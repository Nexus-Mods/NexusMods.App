using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Resources;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.WebAPI;
using NexusMods.Paths;
using StardewModdingAPI.Toolkit;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Emitters;

[UsedImplicitly]
public class VersionDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IOSInformation _os;
    private readonly ISMAPIWebApi _smapiWebApi;
    private readonly IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest> _manifestPipeline;

    public VersionDiagnosticEmitter(
        IServiceProvider serviceProvider,
        ILogger<VersionDiagnosticEmitter> logger,
        IOSInformation os,
        ISMAPIWebApi smapiWebApi)
    {
        _logger = logger;
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

        var smapiMods = await Helpers
            .GetAllManifestsAsync(_logger, loadout, _manifestPipeline, onlyEnabledMods: true, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var apiMods = await _smapiWebApi.GetModDetails(
            os: _os,
            gameVersion,
            smapiVersion,
            smapiIDs: smapiMods.Select(tuple => tuple.Item2.UniqueID).ToArray()
        );

        foreach (var tuple in smapiMods)
        {
            var (smapiMod, manifest) = tuple;

            var minimumApiVersion = manifest.MinimumApiVersion;
            var minimumGameVersion = manifest.MinimumGameVersion;

            if (minimumApiVersion is not null && smapiVersion.IsOlderThan(minimumApiVersion))
            {
                yield return Diagnostics.CreateSMAPIVersionOlderThanMinimumAPIVersion(
                    SMAPIMod: smapiMod.AsLoadoutItemGroup().ToReference(loadout),
                    SMAPIModName: smapiMod.AsLoadoutItemGroup().AsLoadoutItem().Name,
                    MinimumAPIVersion: minimumApiVersion.ToString(),
                    CurrentSMAPIVersion: smapiVersion.ToString(),
                    NexusModsLink: apiMods.GetLink(manifest.UniqueID, defaultValue: Helpers.NexusModsLink),
                    SMAPINexusModsLink: Helpers.SMAPILink
                );
            }

            if (minimumGameVersion is not null && gameVersion.IsOlderThan(minimumGameVersion))
            {
                yield return Diagnostics.CreateGameVersionOlderThanModMinimumGameVersion(
                    SMAPIMod: smapiMod.AsLoadoutItemGroup().ToReference(loadout),
                    SMAPIModName: smapiMod.AsLoadoutItemGroup().AsLoadoutItem().Name,
                    MinimumGameVersion: minimumGameVersion.ToString(),
                    CurrentGameVersion: gameVersion.ToString(),
                    NexusModsLink: apiMods.GetLink(manifest.UniqueID, defaultValue: Helpers.NexusModsLink)
                );
            }
        }
    }
}
