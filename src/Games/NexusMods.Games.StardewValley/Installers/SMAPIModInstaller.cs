using System.Text.Json;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for mods that use the Stardew Modding API (SMAPI).
/// </summary>
public class SMAPIModInstaller : AModInstaller
{
    private static readonly RelativePath ModsFolder = "Mods".ToRelativePath();
    private static readonly RelativePath ManifestFile = "manifest.json".ToRelativePath();

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    private SMAPIModInstaller(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <summary>
    /// Creates a new instance of <see cref="SMAPIModInstaller"/>.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static SMAPIModInstaller Create(IServiceProvider serviceProvider) => new(serviceProvider);

    private static IAsyncEnumerable<(FileTreeNode<RelativePath, ModSourceFileEntry>, SMAPIManifest)> GetManifestFiles(
        FileTreeNode<RelativePath, ModSourceFileEntry> files)
    {
        return files.GetAllDescendentFiles()
            .SelectAsync(async kv =>
        {
            var (path, file) = kv;

            if (!path.FileName.Equals(ManifestFile))
                return default;

            await using var stream = await file!.Open();

            return (kv, await JsonSerializer.DeserializeAsync<SMAPIManifest>(stream));
        })
            .Where(manifest => manifest.Item2 != null)
            .Select(m => (m.kv, m.Item2!));
    }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var manifestFiles = await GetManifestFiles(archiveFiles)
            .ToArrayAsync(cancellationToken: cancellationToken);

        if (!manifestFiles.Any())
            return NoResults;

        var mods = manifestFiles
            .Select(found =>
            {
                var (manifestFile, manifest) = found;
                var parent = manifestFile.Parent;

                var modFiles = parent.GetAllDescendentFiles()
                    .Select(kv =>
                    {
                        var (path, file) = kv;
                        return file!.ToFromArchive(
                            new GamePath(GameFolderType.Game, ModsFolder.Join(path.DropFirst(parent.Depth - 1)))
                        );
                    });

                return new ModInstallerResult
                {
                    Id = ModId.New(),
                    Files = modFiles,
                    Name = manifest.Name,
                    Version = manifest.Version.ToString()
                };
            });

        return mods;
    }

}
