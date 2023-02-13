using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// Matches mods that have all the .archive files in the base folder, optionally with other documentation files.
/// </summary>
public class FolderlessModInstaller : IModInstaller
{
    
    private static Extension[] _supportedExtensions =
    {
        Ext.Archive
    };
    
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<Cyberpunk2077>())
            return Common.Priority.None;
        
        // Make sure we don't have any files that are not supported
        if (!files.All(f => 
                           _supportedExtensions.Contains(f.Key.Extension) || Helpers.IgnoreExtensions.Contains(f.Key.Extension)))
            return Common.Priority.None;

        // If all the files are in the base folder, then we can install them
        if (files.All(f => f.Key.Depth == 1))
            return Common.Priority.Normal;
        
        // If all the files are in the same subfolder, then we can install them
        var firstFile = files.First().Key;
        if (firstFile.Depth > 1)
        {
            var parentFolder = firstFile.Parent;
            if (files.All(f => f.Key.Depth == firstFile.Depth &&
                               f.Key.InFolder(parentFolder)))
                return Common.Priority.Normal;
        }

        // Otherwise, we can't install them
        return Common.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        foreach (var (path, file) in files)
        {
            if (Helpers.IgnoreExtensions.Contains(path.Extension))
                continue;
            
            yield return new FromArchive
            {
                Id = ModFileId.New(),
                From = new HashRelativePath(srcArchive, path),
                To = new GamePath(GameFolderType.Game, @"archive\pc\mod\".ToRelativePath().Join(path.FileName)),
                Hash = file.Hash,
                Size = file.Size,
                Store = file.Store
            };
        }
    }
}