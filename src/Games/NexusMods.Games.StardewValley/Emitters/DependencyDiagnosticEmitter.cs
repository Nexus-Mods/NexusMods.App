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

        var knownUniqueIds = modIdToManifest
            .Select(x => x.Value.UniqueID)
            .ToHashSet();

        var diagnostics = DiagnoseMissingDependencies(loadout, modIdToManifest, knownUniqueIds).ToList();

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

    private static IEnumerable<Diagnostic> DiagnoseMissingDependencies(
        Loadout loadout,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        HashSet<string> knownUniqueIds)
    {
        var modsWithMissingDependencies = modIdToManifest
            .Select(kv =>
                {
                    var (modId, manifest) = kv;
                    var requiredDependencies = manifest.Dependencies.Where(x => x.IsRequired);
                    var missingDependencies = requiredDependencies.Where(x => !knownUniqueIds.Contains(x.UniqueID)).ToList();

                    return (Id: modId, MissingDependencies: missingDependencies);
                }
            )
            .Where(kv => kv.MissingDependencies.Count != 0)
            .ToDictionary(kv => kv.Id, kv => kv.MissingDependencies);

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
