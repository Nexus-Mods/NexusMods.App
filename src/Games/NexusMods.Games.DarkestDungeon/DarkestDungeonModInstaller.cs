using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
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
    private readonly RelativePath _modFilesTxt = "modfiles.txt".ToRelativePath();
    private RelativePath _modFolder = "mods".ToRelativePath();

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (installation.Game is not DarkestDungeon)
            return Common.Priority.None;

        return files.Keys.Any(f => f.FileName == _modFilesTxt)
            ? Common.Priority.Normal
            : Common.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var modFolder = files.Keys.First(m => m.FileName == _modFilesTxt).Parent;
        return files.Where(f => f.Key.InFolder(modFolder))
            .Select(f =>
            new FromArchive
            {
                Id = ModFileId.New(),
                To = new GamePath(GameFolderType.Game, _modFolder.Join(f.Key)),
                From = new HashRelativePath(srcArchive, f.Key),
                Hash = f.Value.Hash,
                Size = f.Value.Size
            });
    }
}
