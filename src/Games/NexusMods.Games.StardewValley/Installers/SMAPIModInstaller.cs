using System.Diagnostics;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.StardewValley.Analyzers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for mods that use the Stardew Modding API (SMAPI).
/// </summary>
public class SMAPIModInstaller : IModInstaller
{
    private static readonly RelativePath ModsFolder = "Mods".ToRelativePath();
    private static readonly RelativePath ManifestFile = "manifest.json".ToRelativePath();

    private static IEnumerable<KeyValuePair<RelativePath, AnalyzedFile>> GetManifestFiles(
        EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return files.Where(kv =>
        {
            var (path, file) = kv;

            if (!path.FileName.Equals(ManifestFile)) return false;
            var manifest = file.AnalysisData
                .OfType<SMAPIManifest>()
                .FirstOrDefault();

            return manifest is not null;
        });
    }

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<StardewValley>()) return Priority.None;
        return GetManifestFiles(archiveFiles).Any()
            ? Priority.Highest
            : Priority.None;
    }

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(srcArchiveHash, archiveFiles));
    }

    private IEnumerable<ModInstallerResult> GetMods(
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var manifestFiles = GetManifestFiles(archiveFiles).ToArray();
        if (!manifestFiles.Any())
            throw new UnreachableException($"{nameof(SMAPIModInstaller)} should guarantee with {nameof(GetPriority)} that {nameof(GetModsAsync)} is never called for archives that don't have a SMAPI manifest file.");

        var mods = manifestFiles
            .Select(manifestFile =>
            {
                var parent = manifestFile.Key.Parent;
                var manifest = manifestFile.Value.AnalysisData
                    .OfType<SMAPIManifest>()
                    .FirstOrDefault();

                if (manifest is null) throw new UnreachableException();

                var modFiles = archiveFiles
                    .Where(kv => kv.Key.InFolder(parent))
                    .Select(kv =>
                    {
                        var (path, file) = kv;
                        return file.ToFromArchive(
                            new GamePath(GameFolderType.Game, ModsFolder.Join(path.DropFirst(parent.Depth)))
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
