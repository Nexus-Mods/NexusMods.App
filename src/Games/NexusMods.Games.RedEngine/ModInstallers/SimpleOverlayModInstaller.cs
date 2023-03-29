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

namespace NexusMods.Games.RedEngine.ModInstallers;

public class SimpleOverlayModInstaller : IModInstaller
{
    private static RelativePath[] _rootPaths = new[]
    {
        "bin/x64",
        "engine",
        "r6",
        "red4ext",
        "archive/pc/mod"
    }.Select(x => x.ToRelativePath()).ToArray();

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<Cyberpunk2077>()) return Common.Priority.None;

        var sets = RootFolder(files);
        return sets.Count != 1 ? Common.Priority.None : Common.Priority.Normal;
    }

    private HashSet<int> RootFolder(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var filtered = files.Where(f => !Helpers.IgnoreExtensions.Contains(f.Key.Extension));

        var sets = filtered.Select(f => _rootPaths.SelectMany(root => GetOffsets(f.Key, root)).ToHashSet())
            .Aggregate((set, x) =>
            {
                set.IntersectWith(x);
                return set;
            });
        return sets;
    }

    /// <summary>
    /// Returns the offsets of which subSection is a subfolder of basePath.
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="subSection"></param>
    /// <returns></returns>
    private IEnumerable<int> GetOffsets(RelativePath basePath, RelativePath subSection)
    {
        var depth = 0;
        while (true)
        {
            if (basePath.Depth == 0) // root
                yield break;

            if (basePath.Depth < subSection.Depth)
                yield break;

            if (basePath == subSection)
                yield return depth;

            if (basePath.StartsWith(subSection))
                yield return depth;

            basePath = basePath.DropFirst();
            depth++;
        }
    }

    public IEnumerable<AModFile> GetFilesToExtract(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var root = RootFolder(files).First();
        var filtered = files.Where(f => !Helpers.IgnoreExtensions.Contains(f.Key.Extension));

        foreach (var (path, file) in filtered)
        {
            yield return new FromArchive()
            {
                Id = ModFileId.New(),
                From = new HashRelativePath(srcArchive, path),
                To = new GamePath(GameFolderType.Game, path.DropFirst(root)),
                Hash = file.Hash,
                Size = file.Size
            };
        }
    }
}
