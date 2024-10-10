using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.Obsidian.FalloutNewVegas.Installers;

public class NVSEInstaller(IServiceProvider serviceProvider) : ALibraryArchiveInstaller(serviceProvider, serviceProvider.GetRequiredService<ILogger<NVSEInstaller>>())
{
    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var tree = libraryArchive.GetTree();
        var keys = new[] { "nvse_1_4.dll", "nvse_editor_1_4.dll", "nvse_loader.exe", "nvse_steam_loader.dll" };


        List<LoadoutFile.New> results = [];
        foreach (var fileNode in tree.EnumerateFilesBfs())
        {
            var relativePath = new RelativePath(fileNode.Value.Item.Path.Name);
            if (!keys.Contains(relativePath.Name.ToString()))
            {
                continue;
            };

            var loadoutFile = new LoadoutFile.New(transaction, out var id)
            {
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                {
                    TargetPath = (loadout.Id, LocationId.Game, relativePath),
                    LoadoutItem = new LoadoutItem.New(transaction, id)
                    {
                        Name = relativePath.Name,
                        LoadoutId = loadout.Id,
                        ParentId = loadoutGroup.Id,
                    },
                },
                Hash = fileNode.Value.Item.LibraryFile.Value.Hash,
                Size = fileNode.Value.Item.LibraryFile.Value.Size,
            };
            results.Add(loadoutFile);
        }

        return results.Count > 0
            ? ValueTask.FromResult<InstallerResult>(new Success())
            : ValueTask.FromResult<InstallerResult>(new NotSupported());
    }
}
