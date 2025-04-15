using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
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
    private readonly IResourceLoader<SMAPIManifestLoadoutFile.ReadOnly, SMAPIManifest> _manifestPipeline;

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
        var gameVersion = Helpers.GetGameVersion(loadout);

        if (!Helpers.TryGetSMAPI(loadout, out var smapi)) yield break;
        if (!SMAPILoadoutItem.Version.TryGetValue(smapi, out var smapiStrVersion)) yield break;
        if (!SemanticVersion.TryParse(smapiStrVersion, out var smapiVersion))
        {
            _logger.LogError("Unable to parse `{Version}` as a semantic version", smapiStrVersion);
            yield break;
        }

        var loadoutItemIdToManifest = (await Helpers
            .GetAllManifestsAsync(_logger, loadout.Db, loadout, onlyEnabled: false, _manifestPipeline, cancellationToken))
            .ToDictionary(static tuple => tuple.Item1.SMAPIManifestLoadoutFileId, static tuple => tuple.Item2);

        var uniqueIdToLoadoutItemId = loadoutItemIdToManifest
            .GroupBy(kv => kv.Value.UniqueID, StringComparer.OrdinalIgnoreCase)
                .Select(g => SelectUniqueIdWinner(loadout, g))
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

    private static KeyValuePair<SMAPIManifestLoadoutFileId, SMAPIManifest> SelectUniqueIdWinner(
        Loadout.ReadOnly loadout, 
        IGrouping<string, KeyValuePair<SMAPIManifestLoadoutFileId, SMAPIManifest>> g)
    {
        // TODO: select winner based on synchronizer winner
        // For now prefer enabled mods over disabled ones, newer versions over older ones
        var sortedItems = g.OrderByDescending(kv => kv.Value.Version).ToArray();
        var enabledItem = sortedItems.FirstOrOptional(kv => LoadoutItem.Load(loadout.Db, kv.Key).IsEnabled());
        return enabledItem.HasValue ? enabledItem.Value : sortedItems.First();
    }

    private static IEnumerable<Diagnostic> DiagnoseDisabledDependencies(
        Loadout.ReadOnly loadout,
        Dictionary<SMAPIManifestLoadoutFileId, SMAPIManifest> loadoutItemIdToManifest,
        ImmutableDictionary<string, SMAPIManifestLoadoutFileId> uniqueIdToLoadoutItemId)
    {
        var collect = loadoutItemIdToManifest
            .Where(kv =>
            {
                var (loadoutItemId, _) = kv;
                var loadoutItem = LoadoutItem.Load(loadout.Db, loadoutItemId);
                return loadoutItem.IsEnabled();
            })
            .Select(kv =>
            {
                var (loadoutItemId, manifest) = kv;

                var requiredDependencies = GetRequiredDependencies(manifest);

                // ReSharper disable once SuggestVarOrType_Elsewhere
                LoadoutItemGroup.ReadOnly[] disabledDependencies = requiredDependencies
                    .Select(uniqueIdToLoadoutItemId.GetValueOrDefault)
                    .Where(id => id != default(SMAPIManifestLoadoutFileId))
                    .Select(id => SMAPIManifestLoadoutFile.Load(loadout.Db, id))
                    .Where(loadoutItem => loadoutItem.IsValid() && !loadoutItem.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().IsEnabled())
                    .Select(loadoutItem => loadoutItem.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent)
                    .ToArray();

                return (Id: loadoutItemId, DisabledDependencies: disabledDependencies);
            })
            .ToArray();

        return collect.SelectMany(tuple =>
        {
            var (loadoutItemId, disabledDependencies) = tuple;
            var loadoutItem = LoadoutItem.Load(loadout.Db, loadoutItemId);

            return disabledDependencies.Select(dependency => Diagnostics.CreateDisabledRequiredDependency(
                SMAPIMod: loadoutItem.Parent.ToReference(loadout),
                Dependency: dependency.ToReference(loadout)
            ));
        });
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseMissingDependencies(
        Loadout.ReadOnly loadout,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        Dictionary<SMAPIManifestLoadoutFileId, SMAPIManifest> loadoutItemIdToManifest,
        ImmutableDictionary<string, SMAPIManifestLoadoutFileId> uniqueIdToLoadoutItemId,
        CancellationToken cancellationToken)
    {
        var collect = loadoutItemIdToManifest
            .Where(kv =>
            {
                var (loadoutItemId, _) = kv;
                return LoadoutItem.Load(loadout.Db, loadoutItemId).IsEnabled();
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
                var loadoutItem = LoadoutItem.Load(loadout.Db, loadoutItemId);
                var modDetails = apiMods.GetValueOrDefault(missingDependency);

                var name = modDetails?.Name ?? missingDependency;
                var link = modDetails is null ? Helpers.NexusModsLink : modDetails.NexusModsLink.ValueOr(() => Helpers.NexusModsLink);

                return Diagnostics.CreateMissingRequiredDependency(
                    SMAPIMod: loadoutItem.Parent.ToReference(loadout),
                    MissingDependencyModId: missingDependency,
                    MissingDependencyModName: name,
                    NexusModsDependencyUri: link
                );
            });
        });
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseOutdatedDependencies(
        Loadout.ReadOnly loadout,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        Dictionary<SMAPIManifestLoadoutFileId, SMAPIManifest> loadoutItemIdToManifest,
        ImmutableDictionary<string, SMAPIManifestLoadoutFileId> uniqueIdToLoadoutItemId,
        CancellationToken cancellationToken)
    {
        var uniqueIdToVersion = loadoutItemIdToManifest
            .Where(kv =>
            {
                var (loadoutItemId, _) = kv;
                return LoadoutItem.Load(loadout.Db, loadoutItemId).IsEnabled();
            })
            .Select(kv => (kv.Value.UniqueID, kv.Value.Version))
            .DistinctBy(kv => kv.UniqueID)
            .ToImmutableDictionary(kv => kv.UniqueID, kv => kv.Version);

        var collect = loadoutItemIdToManifest
            .Where(kv =>
            {
                var (loadoutItemId, _) = kv;
                return LoadoutItem.Load(loadout.Db, loadoutItemId).IsEnabled();
            })
            .SelectMany(kv =>
            {
                var (loadoutItemId, manifest) = kv;

                var minimumVersionDependencies = manifest.Dependencies
                    .Where(x => uniqueIdToVersion.ContainsKey(x.UniqueID) && x.MinimumVersion is not null)
                    .Select(x => (x.UniqueID, x.MinimumVersion))
                    .ToList();

                var contentPack = manifest.ContentPackFor;
                if (contentPack?.MinimumVersion is not null && uniqueIdToVersion.ContainsKey(contentPack.UniqueID))
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
                        }
                    )
                    .Where(tuple => tuple.IsOutdated);
            })
            .ToArray();

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
            Dependent: LoadoutItem.Load(loadout.Db, tuple.LoadoutItemId).Parent.ToReference(loadout),
            Dependency: LoadoutItem.Load(loadout.Db, tuple.DependencyModId).Parent.ToReference(loadout),
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
