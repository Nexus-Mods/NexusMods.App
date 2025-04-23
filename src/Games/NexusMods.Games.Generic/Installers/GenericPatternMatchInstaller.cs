using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.Generic.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.Generic.Installers;

using InstallDataTuple = (LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout);


/// <summary>
/// Generic mod installer for mods that only need to have their contents placed to a specific game location
/// (<see cref="InstallFolderTarget"/>).
/// Tries to match the mod archive folder structure to <see cref="InstallFolderTarget"/> requirements.
///
/// Example: myMod/Textures/myTexture.dds -> Skyrim/Data/Textures/myTexture.dds
/// </summary>
public class GenericPatternMatchInstaller : ALibraryArchiveInstaller
{
    public GenericPatternMatchInstaller(IServiceProvider serviceProvider) :
        base(serviceProvider, serviceProvider.GetRequiredService<ILogger<GenericPatternMatchInstaller>>())
    {
    }

    public InstallFolderTarget[] InstallFolderTargets { get; init; } = [];

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var installDataTuple = (loadoutGroup, transaction, loadout);
        if (InstallFolderTargets.Length == 0)
            return ValueTask.FromResult<InstallerResult>(new NotSupported(Reason: "Found no targets to match against"));

        var tree = libraryArchive.GetTree();

        return InstallFolderTargets.Any(target => TryInstallForTarget(target, tree, installDataTuple))
            ? ValueTask.FromResult<InstallerResult>(new Success())
            : ValueTask.FromResult<InstallerResult>(new NotSupported(Reason: "Found no matching targets"));
    }

    private bool TryInstallForTarget(InstallFolderTarget target, KeyedBox<RelativePath, LibraryArchiveTree> tree, InstallDataTuple installDataTuple)
    {
        foreach (var node in tree.EnumerateChildrenBfs())
        {
            if (!TryGetMatch(node.Value, target, out var match)) continue;
            DoInstall(match ?? tree, target, installDataTuple);
            return true;
        }

        return false;
    }

    private static bool TryGetMatch(KeyedBox<RelativePath, LibraryArchiveTree> node, InstallFolderTarget target, [NotNullWhen(true)] out KeyedBox<RelativePath, LibraryArchiveTree>? match)
    {
        match = null;

        if (node.IsFile())
        {
            // Check if file has a known child file extension
            if (target.KnownValidFileExtensions.Contains(node.Key().Extension))
            {
                match = node.Parent()!;
                return true;
            }
        }
        else
        {
            // Check if the directory name is a known source folder
            if (target.KnownSourceFolderNames.Contains(node.Key().Name))
            {
                match = node;
                return true;
            }

            // Check if the directory name is a known subfolder
            if (target.Names.Contains(node.Key().Name))
            {
                match = node.Parent()!;
                return true;
            }
        }

        return false;
    }

    private void DoInstall(KeyedBox<RelativePath, LibraryArchiveTree> tree, InstallFolderTarget target, InstallDataTuple installDataTuple)
    {
        var dropDepth = tree.Depth();
        var (loadoutGroup, transaction, loadout) = installDataTuple;

        // Discard files and directories based on the target configuration
        var fileNodes = tree.EnumerateFilesBfsWhereBranch(node =>
            {
                if (node.IsDirectory())
                {
                    var relativePath = node.Item.Path.DropFirst(dropDepth);
                    // prune branch if directory is in the discard list
                    if (target.SubPathsToDiscard.Contains(relativePath))
                    {
                        return false;
                    }
                }
                else
                {
                    // prune file if file extension is in the discard list
                    if (target.FileExtensionsToDiscard.Contains(node.Key().Extension))
                    {
                        return false;
                    }
                }

                return true;
            }
        );

        // Add the files to the loadout
        foreach (var fileNode in fileNodes)
        {
            // rebase the path to the target location
            var relativePath = fileNode.Item.Path.DropFirst(dropDepth);
            relativePath = target.DestinationGamePath.Path.Join(relativePath);

            GenerateFileItem(target,
                transaction,
                loadout,
                relativePath,
                loadoutGroup,
                fileNode
            );
        }
    }

    protected virtual void GenerateFileItem(
        InstallFolderTarget target,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        RelativePath relativePath,
        LoadoutItemGroup.New loadoutGroup,
        KeyedBox<RelativePath, LibraryArchiveTree> fileNode)
    {
        var _ = new LoadoutFile.New(transaction, out var id)
        {
            LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
            {
                TargetPath = (loadout.Id, target.DestinationGamePath.LocationId, relativePath),
                LoadoutItem = new LoadoutItem.New(transaction, id)
                {
                    Name = relativePath.Name,
                    LoadoutId = loadout.Id,
                    ParentId = loadoutGroup.Id,
                },
            },
            Hash = fileNode.Item.LibraryFile.Value.Hash,
            Size = fileNode.Item.LibraryFile.Value.Size,
        };
    }
}
