using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine;

public class SimpleOverlyModInstaller : IModInstaller
{
    private static RelativePath[] _rootPaths = new[]
    {
        "bin/x64",
        "engine",
        "r6",
        "red4ext",
        "archive/pc/mod",
        "mods"
    }.Select(x => x.ToRelativePath()).ToArray();

    private static Extension[] _ignoreExtensions = {
        KnownExtensions.Txt,
        KnownExtensions.Md,
        KnownExtensions.Pdf
    };
    
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var filtered = files.Where(f => f.Key.Depth > 1 || !_ignoreExtensions.Contains(f.Key.Extension))
            .Select(f => f.Key);
        
        if (filtered.All(path => _rootPaths.Any(path.InFolder)))
        {
            return Common.Priority.Normal;
        }

        return Common.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var filtered = files.Where(f => f.Key.Depth > 1 || !_ignoreExtensions.Contains(f.Key.Extension));
        
        foreach (var (path, file) in filtered)
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