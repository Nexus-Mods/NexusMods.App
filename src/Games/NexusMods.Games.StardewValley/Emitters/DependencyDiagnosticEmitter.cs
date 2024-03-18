using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.WebAPI;
using NexusMods.Paths;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private static readonly Uri NexusModsPage = new("https://nexusmods.com/stardewvalley");

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

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        var gameVersion = loadout.Installation.Version;
        var smapiMarker = loadout.Mods
            .Where(kv => kv.Value.Enabled)
            .Select(kv => kv.Value.Metadata)
            .Select(metadata => metadata.OfType<SMAPIMarker>().FirstOrDefault())
            .FirstOrDefault(marker => marker is not null);

        if (smapiMarker?.Version is null) yield break;
        var smapiVersion = smapiMarker.Version!;

        var modIdToManifest = await loadout.Mods
            .SelectAsync(async kv => (Id: kv.Key, Manifest: await GetManifest(kv.Value)))
            .Where(tuple => tuple.Manifest is not null)
            .ToDictionaryAsync(x => x.Id, x => x.Manifest!);

        var uniqueIdToModId = modIdToManifest
            .Select(kv => (UniqueId: kv.Value.UniqueID, ModId: kv.Key))
            .ToImmutableDictionary(kv => kv.UniqueId, kv => kv.ModId);

        var a = await DiagnoseMissingDependencies(loadout, gameVersion, smapiVersion, modIdToManifest, uniqueIdToModId);
        var b = await DiagnoseOutdatedDependencies(loadout, gameVersion, smapiVersion, modIdToManifest, uniqueIdToModId);
        var diagnostics = a.Concat(b).ToArray();

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseMissingDependencies(
        Loadout loadout,
        Version gameVersion,
        Version smapiVersion,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId)
    {
        var collect = modIdToManifest.Select(kv =>
        {
            var (modId, manifest) = kv;

            var requiredDependencies = manifest.Dependencies.Where(x => x.IsRequired).Select(x => x.UniqueID);
            var missingDependencies = requiredDependencies.Where(x => !uniqueIdToModId.ContainsKey(x)).ToList();

            var contentPack = manifest.ContentPackFor?.UniqueID;
            if (contentPack is not null && !uniqueIdToModId.ContainsKey(contentPack))
                missingDependencies.Add(contentPack);

            return (Id: modId, MissingDependencies: missingDependencies);
        })
        .Where(kv => kv.MissingDependencies.Count != 0)
        .ToArray();

        var allMissingDependencies = collect
            .SelectMany(kv => kv.MissingDependencies)
            .Distinct()
            .ToArray();

        var missingDependencyUris = await _smapiWebApi.GetModPageUrls(
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
                var mod = loadout.Mods[modId];
                return Diagnostics.CreateMissingRequiredDependency(
                    Mod: mod.ToReference(loadout),
                    MissingDependency: missingDependency,
                    NexusModsDependencyUri: missingDependencyUris.GetValueOrDefault(missingDependency, NexusModsPage).WithName("Nexus Mods")
                );
            });
        });
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseOutdatedDependencies(
        Loadout loadout,
        Version gameVersion,
        Version smapiVersion,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId)
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

        var missingDependencyUris = await _smapiWebApi.GetModPageUrls(
            os: _os,
            gameVersion,
            smapiVersion,
            smapiIDs: allMissingDependencies
        );

        return collect.Select(tuple => Diagnostics.CreateRequiredDependencyIsOutdated(
            Dependent: loadout.Mods[tuple.ModId].ToReference(loadout),
            Dependency: loadout.Mods[tuple.DependencyModId].ToReference(loadout),
            MinimumVersion: tuple.MinimumVersion.ToString(),
            CurrentVersion: tuple.CurrentVersion.ToString(),
            NexusModsDependencyUri: missingDependencyUris.GetValueOrDefault(tuple.DependencyId, NexusModsPage).WithName("Nexus Mods")
        ));
    }

    private async ValueTask<SMAPIManifest?> GetManifest(Mod mod)
    {
        try
        {
            return await Interop.GetManifest(_fileStore, mod);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception trying to get manifest for mod {Mod}", mod.Name);
            return null;
        }
    }
}
