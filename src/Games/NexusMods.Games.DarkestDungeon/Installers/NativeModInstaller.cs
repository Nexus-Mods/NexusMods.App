using System.Diagnostics;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.DarkestDungeon.Models;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.DarkestDungeon.Installers;

/// <summary>
/// <see cref="IModInstaller"/> implementation for native Darkest Dungeon mods with
/// <c>project.xml</c> files.
/// </summary>
public class NativeModInstaller : IModInstaller
{
    private static readonly RelativePath ModsFolder = "mods".ToRelativePath();
    private static readonly RelativePath ProjectFile = "project.xml".ToRelativePath();

    internal static IEnumerable<KeyValuePair<RelativePath, AnalyzedFile>> GetModProjects(
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        return archiveFiles.Where(kv =>
        {
            var (path, file) = kv;

            if (!path.FileName.Equals(ProjectFile)) return false;
            var modProject = file.AnalysisData
                .OfType<ModProject>()
                .FirstOrDefault();

            return modProject is not null;
        });
    }

    public Priority GetPriority(
        GameInstallation installation,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<DarkestDungeon>()) return Priority.None;
        return GetModProjects(archiveFiles).Any()
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

    private static IEnumerable<ModInstallerResult> GetMods(
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var modProjectFiles = GetModProjects(archiveFiles).ToArray();
        if (!modProjectFiles.Any())
            throw new UnreachableException($"{nameof(NativeModInstaller)} should guarantee with {nameof(GetPriority)} that {nameof(GetModsAsync)} is never called for archives that don't have a project.xml file.");

        var mods = modProjectFiles
            .Select(modProjectFile =>
            {
                var parent = modProjectFile.Key.Parent;
                var modProject = modProjectFile.Value.AnalysisData
                    .OfType<ModProject>()
                    .FirstOrDefault();

                if (modProject is null) throw new UnreachableException();

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
                    Name = string.IsNullOrEmpty(modProject.Title) ? null : modProject.Title,
                    Version = modProject.VersionMajor == 0
                        ? null
                        : $"{modProject.VersionMajor}.{modProject.VersionMinor}"
                };
            });

        return mods;
    }
}
