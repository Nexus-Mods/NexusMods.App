using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.StardewValley.Analyzers;

namespace NexusMods.Games.StardewValley.Emitters;

public class MissingDependenciesEmitter : ILoadoutDiagnosticEmitter
{
    public IEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        // TODO: check the versions

        var modIdToManifest = loadout.Mods
            .Select(kv => (Id: kv.Key, Manifest: GetManifest(kv.Value)))
            .Where(tuple => tuple.Manifest is not null)
            .ToDictionary(x => x.Id, x => x.Manifest!);

        var knownUniqueIds = modIdToManifest
            .Select(x => x.Value.UniqueID)
            .ToHashSet();

        var modsWithMissingDependencies = modIdToManifest
            .Select(kv => (Id: kv.Key, MissingDependencies: GetRequiredDependencies(kv.Value)
                    .Where(x => !knownUniqueIds.Contains(x))
                    .ToList()
                ))
            .ToDictionary(x => x.Id, x => x.MissingDependencies);

        foreach (var kv in modsWithMissingDependencies)
        {
            var (modId, missingDependencies) = kv;
            foreach (var missingDependency in missingDependencies)
            {
                var mod = loadout.Mods[modId];
                yield return Diagnostics.MissingRequiredDependency(
                    loadout,
                    mod,
                    missingDependency
                );
            }
        }
    }

    private static IEnumerable<string> GetRequiredDependencies(SMAPIManifest? manifest)
    {
        if (manifest?.Dependencies is null) return Enumerable.Empty<string>();

        var requiredDependencies = manifest.Dependencies
            .Where(x => x.IsRequired)
            .Select(x => x.UniqueID);

        return requiredDependencies;
    }

    private static SMAPIManifest? GetManifest(Mod mod)
    {
        var manifest = mod.Files.Select(kv =>
        {
            var (_, file) = kv;
            var manifest = file.Metadata
                .OfType<SMAPIManifest>()
                .FirstOrDefault();
            return manifest;
        }).FirstOrDefault(x => x is not null);
        return manifest;
    }
}
