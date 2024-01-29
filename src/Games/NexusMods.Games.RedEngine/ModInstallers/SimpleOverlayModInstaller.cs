using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;

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
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        // Note: Expected search space here is small, highest expected overhead is in FindSubPathRootsByKeyUpward.
        // Find all paths which match a known base/root directory.
        var roots = RootPaths
            .SelectMany(x => info.ArchiveFiles.FindSubPathRootsByKeyUpward(x.Parts.ToArray()))
            .OrderBy(node => node.Depth())
            .ToArray();

        if (roots.Length == 0)
            return Array.Empty<ModInstallerResult>();

        var highestRoot = roots.First();
        var newFiles = new List<StoredFile>();

        // Enumerate over all directories with the same depth as the most rooted item.
        foreach (var node in roots.Where(root => root.Depth() == highestRoot.Depth()))
        foreach (var file in node.Item.GetFiles<ModFileTree, RelativePath>())
        {
            // TODO: This can probably be optimised away.
            // For now this is the only use case like this that I (Sewer) have seen while reworking trees.
            // If this becomes more commonplace, I'll add specialised helper.

            var fullPath = file.Path(); // all the way up to root
            var relativePath = fullPath.DropFirst(node.Depth() - 1); // get relative path
            newFiles.Add(new StoredFile()
            {
                Id = ModFileId.NewId(),
                Hash = file.Item.Hash,
                Size = file.Item.Size,
                To = new GamePath(LocationId.Game, relativePath)
            });
        }

        if (newFiles.Count == 0)
            return Array.Empty<ModInstallerResult>();

        return new ModInstallerResult[]{ new()
        {
            Id = info.BaseModId,
            Files = newFiles
        }};
    }
}
