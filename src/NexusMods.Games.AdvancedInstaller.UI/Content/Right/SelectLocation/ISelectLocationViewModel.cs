using System.Collections.ObjectModel;
using DynamicData;

using NexusMods.Sdk.Games;
using NexusMods.UI.Sdk;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

using SelectableTreeNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

/// <summary>
///    View model for showing the user the available locations to install the mod to.
///    Contains the Suggested Locations area and the All Folders trees.
/// </summary>
public interface ISelectLocationViewModel : IViewModelInterface
{
    /// <summary>
    /// Text to display in the Suggested Locations area.
    /// Contains the Game name.
    /// </summary>
    public string SuggestedAreaSubtitle { get; }

    /// <summary>
    /// Observable collection of suggested locations to display in the Suggested Locations area.
    /// </summary>
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }

    /// <summary>
    /// A flat collection of all the Selectable tree entries.
    /// This should be used to add/remove entries from the tree.
    /// </summary>
    public SourceCache<ISelectableTreeEntryViewModel, GamePath> TreeEntriesCache { get; }

    /// <summary>
    /// Observable collection of all the root nodes in the tree.
    /// This is dynamically updated as the user adds/removes entries from the TreeEntriesCache.
    /// </summary>
    public ReadOnlyObservableCollection<SelectableTreeNode> TreeRoots { get; }

    /// <summary>
    /// The tree source to bind to the TreeDataGrid.
    /// </summary>
    public HierarchicalTreeDataGridSource<SelectableTreeNode> Tree { get; }
}
