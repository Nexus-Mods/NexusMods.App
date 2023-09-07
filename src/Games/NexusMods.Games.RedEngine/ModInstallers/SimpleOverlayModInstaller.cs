using DynamicData;
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
using NexusMods.Paths.FileTree;

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

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {

        var roots = RootPaths
            .SelectMany(archiveFiles.FindSubPath)
            .OrderBy(node => node.Depth)
            .ToArray();

        if (roots.Length == 0)
            return Array.Empty<ModInstallerResult>();

        var highestRoot = roots.First();
        var siblings= roots.Where(root => root.Depth == highestRoot.Depth)
            .ToArray();

        var newFiles = new List<FromArchive>();

        foreach (var node in siblings)
        {
            foreach (var (filePath, fileInfo) in node.GetAllDescendentFiles())
            {
                var relativePath = filePath.DropFirst(node.Path.Depth);
                newFiles.Add(new FromArchive()
                {
                    Id = ModFileId.New(),
                    Hash = fileInfo!.Hash,
                    Size = fileInfo.Size,
                    To = new GamePath(GameFolderType.Game, relativePath)
                });
            }
        }

        if (!newFiles.Any())
            return Array.Empty<ModInstallerResult>();

        return new ModInstallerResult[]{ new()
        {
            Id = baseModId,
            Files = newFiles
        }};
    }
}
