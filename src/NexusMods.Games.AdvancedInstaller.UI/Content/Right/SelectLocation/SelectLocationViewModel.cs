using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

using SelectableTreeNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

public class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public string SuggestedAreaSubtitle { get; }
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }
    public HierarchicalTreeDataGridSource<SelectableTreeNode> Tree { get; }
    public ReadOnlyObservableCollection<SelectableTreeNode> TreeRoots => _treeRoots;
    private readonly ReadOnlyObservableCollection<SelectableTreeNode> _treeRoots;

    public SourceCache<ISelectableTreeEntryViewModel, GamePath> TreeEntriesCache { get; } =
        new(entry => entry.GamePath);

    /// <summary>
    /// Constructor.
    /// </summary>
    public SelectLocationViewModel(Loadout.ReadOnly loadout)
    {
        var installation = loadout.InstallationInstance;
        var gameName = installation.Game.Name;
        var register = installation.LocationsRegister;

        SuggestedAreaSubtitle = string.Format(Language.SelectLocationViewModel_SuggestedLocationsSubtitle, gameName);

        SuggestedEntries = CreateSuggestedEntries(register).ToReadOnlyObservableCollection();

        var treeEntries = CreateTreeEntries(register, loadout);

        TreeEntriesCache.AddOrUpdate(treeEntries);

        TreeEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new SelectableTreeNode(node))
            .Bind(out _treeRoots)
            .Subscribe();

        Tree = TreeDataGridHelpers.CreateTreeSourceWithSingleCustomColumn<SelectableTreeNode, ISelectableTreeEntryViewModel, GamePath>(_treeRoots);
    }

    #region private
    /// <summary>
    /// Generates the SuggestedEntries using LocationIds that the game provides.
    /// </summary>
    /// <param name="register"></param>
    /// <returns></returns>
    private static IEnumerable<ISuggestedEntryViewModel> CreateSuggestedEntries(IGameLocationsRegister register)
    {
        List<ISuggestedEntryViewModel> suggestedEntries = new();

        // Add all the top level game locations to suggested entries.
        foreach (var (locationId, fullPath) in register.GetTopLevelLocations())
        {
            suggestedEntries.Add(new SuggestedEntryViewModel(
                id: Guid.NewGuid(),
                absolutePath: fullPath,
                associatedLocation: locationId,
                relativeToTopLevelLocation: new GamePath(locationId, RelativePath.Empty)
            ));

            // Add nested locations to suggested entries.
            foreach (var nestedLocation in register.GetNestedLocations(locationId))
            {
                var nestedFullPath = register.GetResolvedPath(nestedLocation);
                var relativePath = nestedFullPath.RelativeTo(fullPath);
                suggestedEntries.Add(new SuggestedEntryViewModel(
                    id: Guid.NewGuid(),
                    absolutePath: nestedFullPath,
                    associatedLocation: nestedLocation,
                    relativeToTopLevelLocation: new GamePath(locationId, relativePath)
                ));
            }
        }

        return suggestedEntries;
    }

    /// <summary>
    /// Creates the tree entries from the LocationIds and potentially the Loadout folder structure.
    /// </summary>
    /// <param name="register">The game locations register</param>
    /// <param name="loadout">The loadout</param>
    /// <returns>The list of created tree entries that need to be added to the cache.</returns>
    private static List<ISelectableTreeEntryViewModel> CreateTreeEntries(IGameLocationsRegister register, Loadout.ReadOnly loadout)
    {
        // Initial population of the tree based on LocationIds
        List<ISelectableTreeEntryViewModel> treeEntries = new();

        foreach (var (locationId, fullPath) in register.GetTopLevelLocations())
        {
            var treeEntry = new SelectableTreeEntryViewModel(
                new GamePath(locationId, RelativePath.Empty),
                SelectableDirectoryNodeStatus.Regular);

            var createFolderEntry = new SelectableTreeEntryViewModel(
                new GamePath(locationId, ISelectableTreeEntryViewModel.CreateFolderEntryName),
                SelectableDirectoryNodeStatus.Create);

            treeEntries.Add(treeEntry);
            treeEntries.Add(createFolderEntry);

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

        // TODO: Potentially add entries to the tree to represent all the folders found in the loadout.
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
            treeEntries.Add(new SelectableTreeEntryViewModel(
                new GamePath(subPath.LocationId,
                    subPath.Path.Join(ISelectableTreeEntryViewModel.CreateFolderEntryName)),
                SelectableDirectoryNodeStatus.Create));
        }

        return treeEntries;
    }

    #endregion private
}
