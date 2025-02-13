using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Resources;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.WebAPI;
using NexusMods.Paths;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IOSInformation _os;
    private readonly ISMAPIWebApi _smapiWebApi;
    private readonly IResourceLoader<SMAPIModLoadoutItem.ReadOnly, SMAPIManifest> _manifestPipeline;

    public DependencyDiagnosticEmitter(
        IServiceProvider serviceProvider,
        ILogger<DependencyDiagnosticEmitter> logger,
        ISMAPIWebApi smapiWebApi,
        IOSInformation os)
    {
        _logger = logger;
        _smapiWebApi = smapiWebApi;
        _os = os;
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

        var loadoutItemIdToManifest = await Helpers
            .GetAllManifestsAsync(_logger, loadout, _manifestPipeline, onlyEnabledMods: false, cancellationToken)
            .ToDictionaryAsync(tuple => tuple.Item1.SMAPIModLoadoutItemId, tuple => tuple.Item2, cancellationToken);

        var uniqueIdToLoadoutItemId = loadoutItemIdToManifest
            .DistinctBy(kv => kv.Value.UniqueID, StringComparer.OrdinalIgnoreCase)
            .Select(kv => (UniqueId: kv.Value.UniqueID, LoaodutItemId: kv.Key))
            .ToImmutableDictionary(kv => kv.UniqueId, kv => kv.LoaodutItemId, StringComparer.OrdinalIgnoreCase);

        cancellationToken.ThrowIfCancellationRequested();

        var a = DiagnoseDisabledDependencies(loadout, loadoutItemIdToManifest, uniqueIdToLoadoutItemId);
        var b = await DiagnoseMissingDependencies(loadout, gameVersion, smapiVersion, loadoutItemIdToManifest, uniqueIdToLoadoutItemId, cancellationToken);
        var c = await DiagnoseOutdatedDependencies(loadout, gameVersion, smapiVersion, loadoutItemIdToManifest, uniqueIdToLoadoutItemId, cancellationToken);
        var diagnostics = a.Concat(b).Concat(c).ToArray();

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

    private static IEnumerable<Diagnostic> DiagnoseDisabledDependencies(
        Loadout.ReadOnly loadout,
        Dictionary<SMAPIModLoadoutItemId, SMAPIManifest> loadoutItemIdToManifest,
        ImmutableDictionary<string, SMAPIModLoadoutItemId> uniqueIdToLoadoutItemId)
    {
        var collect = loadoutItemIdToManifest
            .Where(kv =>
            {
                var (loadoutItemId, _) = kv;
                var smapiLoadoutItem = SMAPILoadoutItem.Load(loadout.Db, loadoutItemId);
                return smapiLoadoutItem.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled();
            })
            .Select(kv =>
            {
                var (loadoutItemId, manifest) = kv;

                var requiredDependencies = GetRequiredDependencies(manifest);
                var disabledDependencies = requiredDependencies
                    .Select(uniqueIdToLoadoutItemId.GetValueOrDefault)
                    .Where(id => id != default(SMAPIModLoadoutItemId))
                    .Where(id => !SMAPIModLoadoutItem.Load(loadout.Db, id).AsLoadoutItemGroup().AsLoadoutItem().IsEnabled())
                    .ToArray();

                return (Id: loadoutItemId, DisabledDependencies: disabledDependencies);
            })
            .ToArray();

        return collect.SelectMany(tuple =>
        {
            var (loadoutItemId, disabledDependencies) = tuple;
            var smapiMod = SMAPIModLoadoutItem.Load(loadout.Db, loadoutItemId);

            return disabledDependencies.Select(dependencyId => Diagnostics.CreateDisabledRequiredDependency(
                SMAPIMod: smapiMod.AsLoadoutItemGroup().ToReference(loadout),
                Dependency: SMAPIModLoadoutItem.Load(loadout.Db, dependencyId).AsLoadoutItemGroup().ToReference(loadout)
            ));
        });
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseMissingDependencies(
        Loadout.ReadOnly loadout,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        Dictionary<SMAPIModLoadoutItemId, SMAPIManifest> loadoutItemIdToManifest,
        ImmutableDictionary<string, SMAPIModLoadoutItemId> uniqueIdToLoadoutItemId,
        CancellationToken cancellationToken)
    {
        var collect = loadoutItemIdToManifest
            .Where(kv =>
            {
                var (loadoutItemId, _) = kv;
                return SMAPIModLoadoutItem.Load(loadout.Db, loadoutItemId).AsLoadoutItemGroup().AsLoadoutItem().IsEnabled();
            })
            .Select(kv =>
            {
                var (loadoutItemId, manifest) = kv;

                var requiredDependencies = GetRequiredDependencies(manifest);
                var missingDependencies = requiredDependencies
                    .Where(x => !uniqueIdToLoadoutItemId.ContainsKey(x))
                    .ToArray();

                return (Id: loadoutItemId, MissingDependencies: missingDependencies);
            })
            .Where(kv => kv.MissingDependencies.Length != 0)
            .ToArray();

        var allMissingDependencies = collect
            .SelectMany(kv => kv.MissingDependencies)
            .Distinct()
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var apiMods = await _smapiWebApi.GetModDetails(
            os: _os,
            gameVersion,
            smapiVersion,
            smapiIDs: allMissingDependencies
        );

        return collect.SelectMany(kv =>
        {
            var (loadoutItemId, missingDependencies) = kv;
            return missingDependencies.Select(missingDependency =>
            {
                var smapiMod = SMAPIModLoadoutItem.Load(loadout.Db, loadoutItemId);
                var modDetails = apiMods.GetValueOrDefault(missingDependency);
                // TODO: diagnostic even if the API doesn't return anything
                if (modDetails?.Name is null) return null;

                return Diagnostics.CreateMissingRequiredDependency(
                    SMAPIMod: smapiMod.AsLoadoutItemGroup().ToReference(loadout),
                    MissingDependencyModId: modDetails.UniqueId,
                    MissingDependencyModName: modDetails.Name,
                    NexusModsDependencyUri: modDetails.NexusModsLink.ValueOr(() => Helpers.NexusModsLink)
                );
            }).Where(x => x is not null).Select(x => x!);
        });
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseOutdatedDependencies(
        Loadout.ReadOnly loadout,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        Dictionary<SMAPIModLoadoutItemId, SMAPIManifest> loadoutItemIdToManifest,
        ImmutableDictionary<string, SMAPIModLoadoutItemId> uniqueIdToLoadoutItemId,
        CancellationToken cancellationToken)
    {
        var uniqueIdToVersion = loadoutItemIdToManifest
            .Select(kv => (kv.Value.UniqueID, kv.Value.Version))
            .DistinctBy(kv => kv.UniqueID)
            .ToImmutableDictionary(kv => kv.UniqueID, kv => kv.Version);

        var collect = loadoutItemIdToManifest.SelectMany(kv =>
        {
            var (loadoutItemId, manifest) = kv;

            var minimumVersionDependencies = manifest.Dependencies
                .Where(x => uniqueIdToLoadoutItemId.ContainsKey(x.UniqueID) && x.MinimumVersion is not null)
                .Select(x => (x.UniqueID, x.MinimumVersion))
                .ToList();

            var contentPack = manifest.ContentPackFor;
            if (contentPack?.MinimumVersion is not null && uniqueIdToLoadoutItemId.ContainsKey(contentPack.UniqueID))
                minimumVersionDependencies.Add((contentPack.UniqueID, contentPack.MinimumVersion));

            return minimumVersionDependencies.Select(dependency =>
            {
                var dependencyModId = uniqueIdToLoadoutItemId[dependency.UniqueID];

                var minimumVersion = dependency.MinimumVersion!;
                var currentVersion = uniqueIdToVersion[dependency.UniqueID];

                var isOutdated = currentVersion.IsOlderThan(minimumVersion);
                return (
                    LoadoutItemId: loadoutItemId,
                    DependencyModId: dependencyModId,
                    DependencyId: dependency.UniqueID,
                    MinimumVersion: minimumVersion,
                    CurrentVersion: currentVersion,
                    IsOutdated: isOutdated
                );
            })
            .Where(tuple => tuple.IsOutdated);
        }).ToArray();

        var allMissingDependencies = collect
            .Select(tuple => tuple.DependencyId)
            .Distinct()
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var apiMods = await _smapiWebApi.GetModDetails(
            os: _os,
            gameVersion,
            smapiVersion,
            smapiIDs: allMissingDependencies
        );

        return collect.Select(tuple => Diagnostics.CreateRequiredDependencyIsOutdated(
            Dependent: SMAPIModLoadoutItem.Load(loadout.Db, tuple.LoadoutItemId).AsLoadoutItemGroup().ToReference(loadout),
            Dependency: SMAPIModLoadoutItem.Load(loadout.Db, tuple.DependencyModId).AsLoadoutItemGroup().ToReference(loadout),
            MinimumVersion: tuple.MinimumVersion.ToString(),
            CurrentVersion: tuple.CurrentVersion.ToString(),
            NexusModsDependencyUri: apiMods.GetLink(tuple.DependencyId, defaultValue: Helpers.NexusModsLink)
        ));
    }

    private static List<string> GetRequiredDependencies(SMAPIManifest manifest)
    {
        var requiredDependencies = manifest.Dependencies
            .Where(x => x.IsRequired)
            .Select(x => x.UniqueID)
            .ToList();

        var contentPack = manifest.ContentPackFor?.UniqueID;
        if (contentPack is not null) requiredDependencies.Add(contentPack);

        return requiredDependencies;
    }
}
