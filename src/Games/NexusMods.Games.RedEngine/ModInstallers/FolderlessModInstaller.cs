using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// Matches mods that have all the .archive files in the base folder, optionally with other documentation files.
/// </summary>
public class FolderlessModInstaller : ALibraryArchiveInstaller, IModInstaller
{
    public FolderlessModInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<FolderlessModInstaller>>())
    {
    }
    
    private static readonly RelativePath Destination = "archive/pc/mod".ToRelativePath();

    private static readonly HashSet<Extension> IgnoreExtensions = new() {
        KnownExtensions.Txt,
        KnownExtensions.Md,
        KnownExtensions.Pdf,
        KnownExtensions.Png
    };

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var modFiles = info.ArchiveFiles.EnumerateFilesBfs()
            .Where(f => !IgnoreExtensions.Contains(f.Value.Extension()))
            .Select(f => f.Value.ToStoredFile(
                new GamePath(LocationId.Game, Destination.Join(f.Value.FileName()))
            ))
            .ToArray();

        if (!modFiles.Any())
            return [];

        return new[]
        {
            new ModInstallerResult
            {
                Id = info.BaseModId,
                Files = modFiles
            }
        };
    }

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction tx,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var tree = libraryArchive.GetTree();

        var modFiles = tree.EnumerateFilesBfs()
            .Where(f => !IgnoreExtensions.Contains(f.Value.Item.Path.Extension))
            .Select(f => f.Value.ToLoadoutFile(loadout.Id, loadoutGroup.Id, tx, new GamePath(LocationId.Game, Destination.Join(f.Value.Item.Path.FileName))))
            .ToArray();

        return modFiles.Length == 0
            ? ValueTask.FromResult<InstallerResult>(new NotSupported())
            : ValueTask.FromResult<InstallerResult>(new Success());
    }
}
