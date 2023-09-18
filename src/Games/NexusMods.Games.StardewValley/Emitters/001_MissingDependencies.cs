using System.Text.Json;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Emitters;

public class MissingDependenciesEmitter : ILoadoutDiagnosticEmitter
{
    private readonly IArchiveManager _archiveManager;

    public MissingDependenciesEmitter(IArchiveManager archiveManager)
    {
        _archiveManager = archiveManager;
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

        var manifest = await mod.Files
            .Values
            .OfType<IToFile>()
            .Where(f => f.To.FileName == Constants.ManifestFile)
            .OfType<FromArchive>()
            .SelectAsync<FromArchive, SMAPIManifest?>(async fa =>
            {
                try
                {
                    await using var stream = await _archiveManager.GetFileStream(fa.Hash);
                    return await JsonSerializer.DeserializeAsync<SMAPIManifest>(stream);
                }
                catch (Exception)
                {
                    return null;
                }
            }).FirstOrDefaultAsync(m => m != null);
        return manifest;
    }
}
