using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine;

public class SimpleOverlyModInstaller : IModInstaller
{
    private static RelativePath[] _rootPaths = new[]
    {
        "bin/x64",
        "engine",
        "r6",
        "red4ext"
    }.Select(x => x.ToRelativePath()).ToArray();
    
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (files.Keys.All(path => _rootPaths.Any(path.InFolder)))
        {
            return Interfaces.Priority.Normal;
        }

        return Interfaces.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        foreach (var (path, file) in files)
        {
            yield return new FromArchive()
            {
                Id = ModFileId.New(),
                From = new HashRelativePath(srcArchive, path),
                To = new GamePath(GameFolderType.Game, path),
                Hash = file.Hash,
                Size = file.Size,
                Store = file.Store
            };
        }
    }
}