using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.Generic.Installers;

public class PredicateBasedInstaller : ALibraryArchiveInstaller
{
    public PredicateBasedInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<PredicateBasedInstaller>>())
    {
        
    }
    
    public required Func<RelativePath, bool> Root { get; init; }
    public required GamePath Destination { get; init; }
    public required Func<RelativePath, bool> Predicate { get; init; }

    public override ValueTask<InstallerResult> ExecuteAsync(LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var tree = libraryArchive.GetTree();

        var root = tree
            .EnumerateChildrenBfs()
            .SingleOrDefault(x => Root(x.Key));

        var children = tree.EnumerateChildrenBfs()
            .Where(c => c.Value.Item.LibraryFile.HasValue)
            .Where(c => c.Key.InFolder(root.Key))
            .Select(c =>
                {
                    var dest = Destination;
                    var newRelative = dest.Path.Join(c.Key.RelativeTo(root.Key));
                    return (new GamePath(dest.LocationId, newRelative), c.Value.Item.LibraryFile.Value);
                }
            );

        foreach (var (destination, libraryFile) in children)
        {
            _ = new LoadoutFile.New(transaction, out var id)
            {
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                {
                    TargetPath = (loadout.Id, destination.LocationId, destination.Path),
                    LoadoutItem = new LoadoutItem.New(transaction, id)
                    {
                        Name = destination.FileName,
                        LoadoutId = loadout.Id,
                        ParentId = loadoutGroup.Id,
                    },
                },
                Hash = libraryFile.Hash,
                Size = libraryFile.Size,
            };
        }

        return ValueTask.FromResult<InstallerResult>(new Success());
    }
}
