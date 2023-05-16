using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.DarkestDungeon;

public class DarkestDungeonModInstaller : IModInstaller
{
    private static readonly RelativePath ModFilesTxt = "modfiles.txt".ToRelativePath();
    private static readonly RelativePath ModFolder = "mods".ToRelativePath();

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (installation.Game is not DarkestDungeon)
            return Priority.None;

        return archiveFiles.Keys.Any(f => f.FileName == ModFilesTxt)
            ? Priority.Normal
            : Priority.None;
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
        var modFolder = archiveFiles.Keys
            .First(m => m.FileName == ModFilesTxt)
            .Parent;

        var modFiles = archiveFiles
            .Where(kv => kv.Key.InFolder(modFolder))
            .Select(kv =>
            {
                var (path, file) = kv;

                return new FromArchive
                {
                    Id = ModFileId.New(),
                    To = new GamePath(GameFolderType.Game, ModFolder.Join(path)),
                    Hash = file.Hash,
                    Size = file.Size
                };
            });

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }
}
