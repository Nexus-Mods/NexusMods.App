using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Extensions.BCL;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger<DependencyDiagnosticEmitter> _logger;
    private readonly IFileStore _fileStore;

    public DependencyDiagnosticEmitter(
        ILogger<DependencyDiagnosticEmitter> logger,
        IFileStore fileStore)
    {
        _logger = logger;
        _fileStore = fileStore;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        var modIdToManifest = await loadout.Mods
            .SelectAsync(async kv => (Id: kv.Key, Manifest: await GetManifest(kv.Value)))
            .Where(tuple => tuple.Manifest is not null)
            .ToDictionaryAsync(x => x.Id, x => x.Manifest!);

        var uniqueIdToModId = modIdToManifest
            .Select(kv => (UniqueId: kv.Value.UniqueID, ModId: kv.Key))
            .ToImmutableDictionary(kv => kv.UniqueId, kv => kv.ModId);

        var diagnostics = DiagnoseMissingDependencies(loadout, modIdToManifest, uniqueIdToModId)
            .Concat(DiagnoseOutdatedDependencies(loadout, modIdToManifest, uniqueIdToModId))
            .ToList();

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

    private static IEnumerable<Diagnostic> DiagnoseMissingDependencies(
        Loadout loadout,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId)
    {
        var modsWithMissingDependencies = modIdToManifest.Select(kv =>
        {
            var (modId, manifest) = kv;
            var requiredDependencies = manifest.Dependencies.Where(x => x.IsRequired);
            var missingDependencies = requiredDependencies.Where(x => !uniqueIdToModId.ContainsKey(x.UniqueID)).ToList();

            return (Id: modId, MissingDependencies: missingDependencies);
        })
        .Where(kv => kv.MissingDependencies.Count != 0)
        .ToImmutableDictionary(kv => kv.Id, kv => kv.MissingDependencies);

        foreach (var kv in modsWithMissingDependencies)
        {
            var (modId, missingDependencies) = kv;
            foreach (var missingDependency in missingDependencies)
            {
                var mod = loadout.Mods[modId];
                yield return Diagnostics.CreateMissingRequiredDependency(
                    Mod: mod.ToReference(loadout),
                    MissingDependency: missingDependency.UniqueID
                );
            }
        }
    }

    private static IEnumerable<Diagnostic> DiagnoseOutdatedDependencies(
        Loadout loadout,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId)
    {
        var uniqueIdToVersion = modIdToManifest
            .Select(kv => (kv.Value.UniqueID, kv.Value.Version))
            .ToImmutableDictionary(kv => kv.UniqueID, kv => kv.Version);

        return modIdToManifest.SelectMany(kv =>
        {
            var (modId, manifest) = kv;

            var minimumVersionDependencies = manifest.Dependencies.Where(x => uniqueIdToModId.ContainsKey(x.UniqueID) && x.MinimumVersion is not null);
            return minimumVersionDependencies.Select(dependency =>
            {
                var dependencyModId = uniqueIdToModId[dependency.UniqueID];

                var minimumVersion = dependency.MinimumVersion!;
                var currentVersion = uniqueIdToVersion[dependency.UniqueID];

                var isOutdated = currentVersion.IsOlderThan(minimumVersion);
                return (DependencyModId: dependencyModId, MinimumVersion: minimumVersion, CurrentVersion: currentVersion, IsOutdated: isOutdated);
            })
            .Where(tuple => tuple.IsOutdated)
            .Select(tuple => Diagnostics.CreateOutdatedDependency(
                Dependent: loadout.Mods[modId].ToReference(loadout),
                Dependency: loadout.Mods[tuple.DependencyModId].ToReference(loadout),
                MinimumVersion: tuple.MinimumVersion.ToString(),
                CurrentVersion: tuple.CurrentVersion.ToString()
            ));
        });
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
