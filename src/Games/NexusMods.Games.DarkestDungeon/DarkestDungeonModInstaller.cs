using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon;

public class DarkestDungeonModInstaller : IModInstaller
{
    public DarkestDungeonModInstaller(IDataStore store)
    {
        _store = store;
    }
    
    private readonly RelativePath ModFilesTxt = "modfiles.txt".ToRelativePath();
    private readonly RelativePath ModFolder = "mods".ToRelativePath();
    private readonly IDataStore _store;

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (installation.Game is not DarkestDungeon) return Interfaces.Priority.None;

        return files.Keys.Any(f => f.FileName == ModFilesTxt) 
            ? Interfaces.Priority.Normal 
            : Interfaces.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var modFolder = files.Keys.First(m => m.FileName == ModFilesTxt).Parent;
        return files.Where(f => f.Key.InFolder(modFolder))
            .Select(f =>
            new FromArchive{
                To = new GamePath(GameFolderType.Game, ModFolder.Join(f.Key)),
                From = new HashRelativePath(srcArchive, f.Key),
                Hash = f.Value.Hash,
                Size = f.Value.Size,
                Store = _store
            });
    }
}