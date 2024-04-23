using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private static readonly NamedLink NexusModsLink = new("Nexus Mods", new Uri("https://nexusmods.com/stardewvalley"));

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

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var gameVersion = new SemanticVersion(loadout.Installation.Version);
        var smapiMarker = loadout.Mods
            .Where(kv => kv.Value.Enabled)
            .Select(kv => kv.Value.Metadata)
            .Select(metadata => metadata.OfType<SMAPIMarker>().FirstOrDefault())
            .FirstOrDefault(marker => marker is not null);

        if (smapiMarker is null) yield break;
        if (!smapiMarker.TryParse(out var smapiVersion)) yield break;

        var modIdToManifest = await loadout.Mods
            .SelectAsync(async kv => (Id: kv.Key, Manifest: await GetManifest(kv.Value, cancellationToken)))
            .Where(tuple => tuple.Manifest is not null)
            .ToDictionaryAsync(x => x.Id, x => x.Manifest!, cancellationToken);

        var uniqueIdToModId = modIdToManifest
            .Select(kv => (UniqueId: kv.Value.UniqueID, ModId: kv.Key))
            .ToImmutableDictionary(kv => kv.UniqueId, kv => kv.ModId);

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
        Loadout loadout,
        Dictionary<ModId, SMAPIManifest> modIdToManifest,
        ImmutableDictionary<string, ModId> uniqueIdToModId)
    {
        var collect = modIdToManifest
            .Where(kv =>
            {
                var (modId, _) = kv;
                return loadout.Mods[modId].Enabled;
            })
            .Select(kv =>
            {
                var (modId, manifest) = kv;

                var requiredDependencies = GetRequiredDependencies(manifest);
                var disabledDependencies = requiredDependencies
                    .Select(uniqueIdToModId.GetValueOrDefault)
                    .Where(id => id != default(ModId))
                    .Where(id => !loadout.Mods[id].Enabled)
                    .ToArray();

                return (Id: modId, DisabledDependencies: disabledDependencies);
            })
            .ToArray();

        return collect.SelectMany(tuple =>
        {
            var (modId, disabledDependencies) = tuple;
            return disabledDependencies.Select(dependency => Diagnostics.CreateDisabledRequiredDependency(
                Mod: loadout.Mods[modId].ToReference(loadout),
                Dependency: loadout.Mods[dependency].ToReference(loadout)
            ));
        });
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseMissingDependencies(
        Loadout loadout,
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
                return loadout.Mods[modId].Enabled;
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
                    NexusModsDependencyUri: missingDependencyUris.GetValueOrDefault(missingDependency, NexusModsLink)
                );
            });
        });
    }

    private async Task<IEnumerable<Diagnostic>> DiagnoseOutdatedDependencies(
        Loadout loadout,
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
            NexusModsDependencyUri: missingDependencyUris.GetValueOrDefault(tuple.DependencyId, NexusModsLink)
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

    private async ValueTask<SMAPIManifest?> GetManifest(Mod mod, CancellationToken cancellationToken)
    {
        try
        {
            return await Interop.GetManifest(_fileStore, mod, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignored
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception trying to get manifest for mod {Mod}", mod.Name);
            return null;
        }
    }
}
