using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
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
