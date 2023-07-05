using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
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
    private static readonly RelativePath[] RootPaths = new[]
        {
            "bin/x64",
            "engine",
            "r6",
            "red4ext",
            "archive/pc/mod"
        }
        .Select(x => x.ToRelativePath())
        .ToArray();

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<Cyberpunk2077>()) return Priority.None;

        var sets = RootFolder(archiveFiles);
        return sets.Count != 1 ? Priority.None : Priority.Normal;
    }

    private static HashSet<int> RootFolder(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var filtered = files.Where(f => !Helpers.IgnoreExtensions.Contains(f.Key.Extension));

        var sets = filtered.Select(f => RootPaths.SelectMany(root => GetOffsets(f.Key, root)).ToHashSet())
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
    private static IEnumerable<int> GetOffsets(RelativePath basePath, RelativePath subSection)
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
        var root = RootFolder(archiveFiles).First();

        var modFiles = archiveFiles
            .Where(kv => !Helpers.IgnoreExtensions.Contains(kv.Key.Extension))
            .Select(kv =>
            {
                var (path, file) = kv;
                return file.ToFromArchive(
                    new GamePath(GameFolderType.Game, path.DropFirst(root))
                );
            });

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }
}
