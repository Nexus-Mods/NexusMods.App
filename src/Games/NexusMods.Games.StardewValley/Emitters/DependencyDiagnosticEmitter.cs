using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.WebAPI;
using NexusMods.Paths;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger<DependencyDiagnosticEmitter> _logger;
    private readonly IFileStore _fileStore;
    private readonly IOSInformation _os;
    private readonly ISMAPIWebApi _smapiWebApi;

    public DependencyDiagnosticEmitter(
        ILogger<DependencyDiagnosticEmitter> logger,
        IFileStore fileStore,
        ISMAPIWebApi smapiWebApi,
        IOSInformation os)
    {
        _logger = logger;
        _fileStore = fileStore;
        _smapiWebApi = smapiWebApi;
        _os = os;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var gameVersion = new SemanticVersion(loadout.InstallationInstance.Version);
        var optionalSMAPIMod = loadout.GetFirstModWithMetadata(SMAPIMarker.Version);
        if (!optionalSMAPIMod.HasValue) yield break;

        var (_, smapiMarker) = optionalSMAPIMod.Value;
        if (!SemanticVersion.TryParse(smapiMarker, out var smapiVersion)) yield break;

        var modIdToManifest = await Helpers
            .GetAllManifestsAsync(_logger, _fileStore, loadout, onlyEnabledMods: false, cancellationToken)
            .ToDictionaryAsync(tuple => ModId.From(tuple.Item1.Id), tuple => tuple.Item2, cancellationToken);

        var uniqueIdToModId = modIdToManifest
            .DistinctBy(kv => kv.Value.UniqueID, StringComparer.OrdinalIgnoreCase)
            .Select(kv => (UniqueId: kv.Value.UniqueID, ModId: kv.Key))
            .ToImmutableDictionary(kv => kv.UniqueId, kv => kv.ModId, StringComparer.OrdinalIgnoreCase);

        cancellationToken.ThrowIfCancellationRequested();

        var a = DiagnoseDisabledDependencies(loadout, modIdToManifest, uniqueIdToModId);
        var b = await DiagnoseMissingDependencies(loadout, gameVersion, smapiVersion, modIdToManifest, uniqueIdToModId, cancellationToken);
        var c = await DiagnoseOutdatedDependencies(loadout, gameVersion, smapiVersion, modIdToManifest, uniqueIdToModId, cancellationToken);
        var diagnostics = a.Concat(b).Concat(c).ToArray();

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }
    
    private static IEnumerable<Diagnostic> DiagnoseDisabledDependencies(
        Loadout.ReadOnly loadout,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId)
    {
        
        var collect = modIdToManifest
            .Where(kv =>
            {
                var (modId, _) = kv;
                return ModForId(loadout, modId).Enabled;
            })
            .Select(kv =>
            {
                var (modId, manifest) = kv;

                var requiredDependencies = GetRequiredDependencies(manifest);
                var disabledDependencies = requiredDependencies
                    .Select(uniqueIdToModId.GetValueOrDefault)
                    .Where(id => id != default(ModId))
                    .Where(id => !ModForId(loadout, modId).Enabled)
                    .ToArray();

                return (Id: modId, DisabledDependencies: disabledDependencies);
            })
            .ToArray();

        return collect.SelectMany(tuple =>
        {
            var (modId, disabledDependencies) = tuple;
            return disabledDependencies.Select(dependency => Diagnostics.CreateDisabledRequiredDependency(
                Mod: ModForId(loadout, modId).ToReference(loadout),
                Dependency: ModForId(loadout, dependency).ToReference(loadout)
            ));
        });
    }

    private static Mod.ReadOnly ModForId(Loadout.ReadOnly loadout, ModId id)
    {
        return Mod.Load(loadout.Db, id);
    }
    
    private async Task<IEnumerable<Diagnostic>> DiagnoseMissingDependencies(
        Loadout.ReadOnly loadout,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId,
        CancellationToken cancellationToken)
    {
        var collect = modIdToManifest
            .Where(kv =>
            {
                var (modId, _) = kv;
                return ModForId(loadout, modId).Enabled;
            })
            .Select(kv =>
            {
                var (modId, manifest) = kv;

                var requiredDependencies = GetRequiredDependencies(manifest);
                var missingDependencies = requiredDependencies
                    .Where(x => !uniqueIdToModId.ContainsKey(x))
                    .ToArray();

                return (Id: modId, MissingDependencies: missingDependencies);
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
            var (modId, missingDependencies) = kv;
            return missingDependencies.Select(missingDependency =>
            {
                var mod = ModForId(loadout, modId);
                var modDetails = apiMods.GetValueOrDefault(missingDependency);
                // TODO: diagnostic even if the API doesn't return anything
                if (modDetails?.Name is null) return null;

                return Diagnostics.CreateMissingRequiredDependency(
                    Mod: mod.ToReference(loadout),
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
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId,
        CancellationToken cancellationToken)
    {
        var uniqueIdToVersion = modIdToManifest
            .Select(kv => (kv.Value.UniqueID, kv.Value.Version))
            .ToImmutableDictionary(kv => kv.UniqueID, kv => kv.Version);

        var collect = modIdToManifest.SelectMany(kv =>
        {
            var (modId, manifest) = kv;

            var minimumVersionDependencies = manifest.Dependencies
                .Where(x => uniqueIdToModId.ContainsKey(x.UniqueID) && x.MinimumVersion is not null)
                .Select(x => (x.UniqueID, x.MinimumVersion))
                .ToList();

            var contentPack = manifest.ContentPackFor;
            if (contentPack?.MinimumVersion is not null && uniqueIdToModId.ContainsKey(contentPack.UniqueID))
                minimumVersionDependencies.Add((contentPack.UniqueID, contentPack.MinimumVersion));

            return minimumVersionDependencies.Select(dependency =>
            {
                var dependencyModId = uniqueIdToModId[dependency.UniqueID];

                var minimumVersion = dependency.MinimumVersion!;
                var currentVersion = uniqueIdToVersion[dependency.UniqueID];

                var isOutdated = currentVersion.IsOlderThan(minimumVersion);
                return (
                    ModId: modId,
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
            Dependent: ModForId(loadout, tuple.ModId).ToReference(loadout),
            Dependency: ModForId(loadout, tuple.DependencyModId).ToReference(loadout),
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
