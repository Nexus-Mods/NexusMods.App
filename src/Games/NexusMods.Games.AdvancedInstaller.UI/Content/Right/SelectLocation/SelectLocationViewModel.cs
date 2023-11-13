using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }

    public ReadOnlyObservableCollection<TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>> TreeRoots =>
        _treeRoots;
    private readonly ReadOnlyObservableCollection<TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>> _treeRoots;
    public SourceCache<ISelectableTreeEntryViewModel, GamePath> TreeEntriesCache { get; } =
        new(entry => entry.GamePath);

    public ReadOnlyObservableCollection<ILocationTreeContainerViewModel> TreeContainers => _treeContainers;
    private readonly ReadOnlyObservableCollection<ILocationTreeContainerViewModel> _treeContainers;


    public SelectLocationViewModel(GameLocationsRegister register, Loadout? loadout)
    {
        SuggestedEntries = CreateSuggestedEntries(register).ToReadOnlyObservableCollection();

        var treeEntries = CreateTreeEntries(register, loadout);
        // For each entry, create a CreateFolder entry.
        var createFolderEntries = treeEntries.Select(existingNode => new SelectableTreeEntryViewModel(
            new GamePath(existingNode.GamePath.LocationId, existingNode.GamePath.Path.Join("*CreateFolder*")),
            SelectableDirectoryNodeStatus.Create));


        TreeEntriesCache.AddOrUpdate(treeEntries);
        TreeEntriesCache.AddOrUpdate(createFolderEntries);
        TreeEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>(node))
            .Bind(out _treeRoots)
            .Subscribe();

        _treeRoots.ToObservableChangeSet()
            .Transform(treeNode => (ILocationTreeContainerViewModel) new LocationTreeContainerViewModel(treeNode))
            .Bind(out _treeContainers)
            .Subscribe();
    }


    /// <summary>
    /// Generates the SuggestedEntries using LocationIds that the game provides.
    /// </summary>
    /// <param name="register"></param>
    /// <returns></returns>
    private static IEnumerable<ISuggestedEntryViewModel> CreateSuggestedEntries(GameLocationsRegister register)
    {
        List<ISuggestedEntryViewModel> suggestedEntries = new();

        // Add all the top level game locations to suggested entries.
        foreach (var (locationId, fullPath) in register.GetTopLevelLocations())
        {
            suggestedEntries.Add(new SuggestedEntryViewModel(
                Guid.NewGuid(),
                fullPath,
                locationId,
                new GamePath(locationId, RelativePath.Empty)));

            // Add nested locations to suggested entries.
            foreach (var nestedLocation in register.GetNestedLocations(locationId))
            {
                var nestedFullPath = register.GetResolvedPath(nestedLocation);
                var relativePath = nestedFullPath.RelativeTo(fullPath);
                suggestedEntries.Add(new SuggestedEntryViewModel(
                    Guid.NewGuid(),
                    nestedFullPath,
                    nestedLocation,
                    new GamePath(locationId, relativePath)));
            }
        }

        return suggestedEntries;
    }

    private static List<ISelectableTreeEntryViewModel> CreateTreeEntries(GameLocationsRegister register,
        Loadout? loadout)
    {
        // Initial population of the tree based on LocationIds
        List<ISelectableTreeEntryViewModel> treeEntries = new();

        foreach (var (locationId, fullPath) in register.GetTopLevelLocations())
        {
            var treeEntry = new SelectableTreeEntryViewModel(
                new GamePath(locationId, RelativePath.Empty),
                SelectableDirectoryNodeStatus.Regular);

            treeEntries.Add(treeEntry);

            // Add nested
            foreach (var nestedLocation in register.GetNestedLocations(locationId))
            {
                var nestedFullPath = register.GetResolvedPath(nestedLocation);
                var relativePath = nestedFullPath.RelativeTo(fullPath);
                var relativeGamePath = new GamePath(locationId, relativePath);
                // Add all nodes from root to nested location.
                treeEntries.AddRange(CreateMissingEntriesForGamePath(treeEntries.ToArray(), relativeGamePath, false));
            }
        }

        if (loadout != null)
        {
            // TODO: Potentially add entries to the tree to represent all the folders found in the loadout.
        }

        return treeEntries;
    }

    /// <summary>
    /// Returns a collection of <see cref="ISelectableTreeEntryViewModel" /> that are missing from the passed flat list of entries.
    /// </summary>
    /// <param name="currentEntries">The collection of existing entries to check against</param>
    /// <param name="gamePath">A GamePath relative to a top level location.</param>
    /// <param name="fromMapping">If the newly created elements are transient</param>
    /// <returns></returns>
    private static IEnumerable<ISelectableTreeEntryViewModel> CreateMissingEntriesForGamePath(
        ISelectableTreeEntryViewModel[] currentEntries, GamePath gamePath, bool fromMapping)
    {
        List<ISelectableTreeEntryViewModel> treeEntries = new();
        foreach (var subPath in gamePath.GetAllParents())
        {
            var existingEntry = currentEntries.FirstOrDefault(x => x.GamePath == subPath);
            if (existingEntry != null)
            {
                // If the entry already exists, we don't need to add it again.
                // We assume all parents also already exist.
                break;
            }

            var status = fromMapping
                ? SelectableDirectoryNodeStatus.RegularFromMapping
                : SelectableDirectoryNodeStatus.Regular;

            treeEntries.Add(new SelectableTreeEntryViewModel(subPath, status));
        }

        return treeEntries;
    }
}
