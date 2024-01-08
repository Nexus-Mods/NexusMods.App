using NexusMods.DataModel.Abstractions.Games;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Trees;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// Matches mods that have all the .archive files in the base folder, optionally with other documentation files.
/// </summary>
public class FolderlessModInstaller : IModInstaller
{
    private static readonly RelativePath Destination = "archive/pc/mod".ToRelativePath();

    private static readonly HashSet<Extension> IgnoreExtensions = new() {
        KnownExtensions.Txt,
        KnownExtensions.Md,
        KnownExtensions.Pdf,
        KnownExtensions.Png
    };

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var modFiles = archiveFiles.EnumerateFilesBfs()
            .Where(f => !IgnoreExtensions.Contains(f.Value.Extension()))
            .Select(f => f.Value.ToStoredFile(
                new GamePath(LocationId.Game, Destination.Join(f.Value.FileName()))
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
