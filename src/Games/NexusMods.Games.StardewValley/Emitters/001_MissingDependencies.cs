using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Extensions.BCL;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Emitters;

public class MissingDependenciesEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger<MissingDependenciesEmitter> _logger;
    private readonly IFileStore _fileStore;

    public MissingDependenciesEmitter(
        ILogger<MissingDependenciesEmitter> logger,
        IFileStore fileStore)
    {
        _logger = logger;
        _fileStore = fileStore;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout)
    {
        // TODO: check the versions

        var modIdToManifest = await loadout.Mods
            .SelectAsync(async kv => (Id: kv.Key, Manifest: await GetManifest(kv.Value)))
            .Where(tuple => tuple.Manifest is not null)
            .ToDictionaryAsync(x => x.Id, x => x.Manifest!);

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
