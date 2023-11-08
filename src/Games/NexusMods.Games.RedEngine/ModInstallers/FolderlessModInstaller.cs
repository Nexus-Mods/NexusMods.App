using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// Matches mods that have all the .archive files in the base folder, optionally with other documentation files.
/// </summary>
public class FolderlessModInstaller : IModInstaller
{
    private static readonly RelativePath Destination = "archive/pc/mod".ToRelativePath();

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {

        var modFiles = archiveFiles.GetAllDescendentFiles()
            .Where(f => !Helpers.IgnoreExtensions.Contains(f.Path.Extension))
            .Select(f => f.Value!.ToStoredFile(
                new GamePath(LocationId.Game, Destination.Join(f.Path.FileName))
            ))
            .ToArray();

        if (!modFiles.Any())
            return Enumerable.Empty<ModInstallerResult>();

        return new[]
        {
            new ModInstallerResult
            {
                Id = baseModId,
                Files = modFiles
            }
        };
    }
}
