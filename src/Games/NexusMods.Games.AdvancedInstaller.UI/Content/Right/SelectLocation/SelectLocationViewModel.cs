using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using DynamicData;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
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
    /// Constructs the view model for the Select Location view.
    /// </summary>
    /// <param name="register">The game locations register to obtain the locations.</param>
    /// <param name="loadout">The loadout, to obtain the loadout folder structure. Can be null.</param>
    /// <param name="gameName">The name of the Game, to show in the ui.</param>
    public SelectLocationViewModel(GameLocationsRegister register, Loadout? loadout, string gameName)
    {
        SuggestedAreaSubtitle = string.Format(Language.SelectLocationViewModel_SuggestedLocationsSubtitle, gameName);

        SuggestedEntries = CreateSuggestedEntries(register).ToReadOnlyObservableCollection();

        var treeEntries = CreateTreeEntries(register, loadout);

        // For each entry, create a CreateFolder entry.
        var createFolderEntries = treeEntries.Select(existingNode =>
            new SelectableTreeEntryViewModel(
                new GamePath(existingNode.GamePath.LocationId, existingNode.GamePath.Path.Join("*CreateFolder*")),
                SelectableDirectoryNodeStatus.Create));

        TreeEntriesCache.AddOrUpdate(treeEntries);
        TreeEntriesCache.AddOrUpdate(createFolderEntries);

        TreeEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new SelectableTreeNode(node))
            .Bind(out _treeRoots)
            .Subscribe();

        Tree = GetTreeSource(_treeRoots);
    }

    #region private

    /// <summary>
    /// Generates the Tree source for the TreeDataGrid.
    /// </summary>
    /// <param name="treeRoots">An observable collection of the tree roots.</param>
    /// <returns></returns>
    private static HierarchicalTreeDataGridSource<SelectableTreeNode> GetTreeSource(
        ReadOnlyObservableCollection<SelectableTreeNode> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<SelectableTreeNode>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<SelectableTreeNode>(
                    new TemplateColumn<SelectableTreeNode>(null,
                        new FuncDataTemplate<SelectableTreeNode>((node, _) =>
                            new SelectableTreeEntryView
                            {
                                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                                DataContext = node?.Item,
                            }),
                        width: new GridLength(1, GridUnitType.Star)
                    ),
                    node => node.Children,
                    null,
                    node => node.IsExpanded)
            }
        };
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

    /// <summary>
    /// Creates the tree entries from the LocationIds and potentially the Loadout folder structure.
    /// </summary>
    /// <param name="register">The game locations register</param>
    /// <param name="loadout">The loadout, can be null.</param>
    /// <returns>The list of created tree entries that need to be added to the cache.</returns>
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

    #endregion private
}
