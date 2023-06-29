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
/// <see cref="IModInstaller"/> implementation for loose file mods.
/// </summary>
public class LooseFilesModInstaller : IModInstaller
{
    private static readonly RelativePath ModsFolder = "mods".ToRelativePath();

    public Priority GetPriority(
        GameInstallation installation,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<DarkestDungeon>()) return Priority.None;
        if (NativeModInstaller.GetModProjects(archiveFiles).Any()) return Priority.None;

        // TODO: invalid directory structures: https://github.com/Nexus-Mods/NexusMods.App/issues/325
        return Priority.Lowest;
    }

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(baseModId, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<ModInstallerResult> GetMods(
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var files = archiveFiles
            .Select(kv =>
            {
                var (path, file) = kv;
                return file.ToFromArchive(
                    new GamePath(GameFolderType.Game, ModsFolder.Join(path))
                );
            });

        // TODO: create project.xml file for the mod
        // this needs to be serialized to XML and added to the files enumerable
        var modProject = new ModProject
        {
            Title = archiveFiles.First().Key.TopParent.ToString()
        };

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = files,
        };
    }
}
