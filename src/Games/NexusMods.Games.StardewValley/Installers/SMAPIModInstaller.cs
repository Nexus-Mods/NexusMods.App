using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for mods that use the Stardew Modding API (SMAPI).
/// </summary>
public class SMAPIModInstaller : AModInstaller
{
    private readonly ILogger<SMAPIModInstaller> _logger;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    private SMAPIModInstaller(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<SMAPIModInstaller>>();
    }

    /// <summary>
    /// Creates a new instance of <see cref="SMAPIModInstaller"/>.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static SMAPIModInstaller Create(IServiceProvider serviceProvider) => new(serviceProvider);

    private async ValueTask<List<(KeyedBox<RelativePath, ModFileTree>, SMAPIManifest)>> GetManifestFiles(
        KeyedBox<RelativePath, ModFileTree> files)
    {
        var results = new List<(KeyedBox<RelativePath, ModFileTree>, SMAPIManifest)>();
        foreach (var kv in files.GetFiles())
        {
            if (!kv.FileName().Equals(Constants.ManifestFile)) continue;

            try
            {
                await using var stream = await kv.Item.OpenAsync();
                var manifest = await Interop.DeserializeManifest(stream);
                if (manifest is not null) results.Add((kv, manifest));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception trying to deserialize {File}", kv.Path());
            }
        }

        return results;
    }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var manifestFiles = await GetManifestFiles(info.ArchiveFiles);
        if (manifestFiles.Count == 0)
            return NoResults;

        var mods = manifestFiles
            .Select(found =>
            {
                var (manifestFile, manifest) = found;
                var parent = manifestFile.Parent();

                var modFiles = parent!.Item
                    .GetFiles<ModFileTree, RelativePath>()
                    .Select(kv =>
                        {
                            var storedFile = kv.ToStoredFile(
                                new GamePath(
                                    LocationId.Game,
                                    Constants.ModsFolder.Join(kv.Path().DropFirst(parent.Depth() - 1))
                                )
                            );

                            if (!kv.Equals(manifestFile)) return storedFile;
                            return storedFile with
                            {
                                Metadata = [new SMAPIManifestMetadata()],
                            };
                        }
                    );

                return new ModInstallerResult
                {
                    Id = ModId.NewId(),
                    Files = modFiles,
                    Name = manifest.Name,
                    Version = manifest.Version.ToString(),
                };
            });

        return mods;
    }

}
