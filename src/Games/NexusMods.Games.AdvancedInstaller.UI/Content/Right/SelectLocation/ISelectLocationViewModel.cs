using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

using SelectableTreeNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

public interface ISelectLocationViewModel : IViewModelInterface
{
    public string SuggestedAreaSubtitle { get; }

    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }

    public HierarchicalTreeDataGridSource<SelectableTreeNode> Tree { get; }

    public ReadOnlyObservableCollection<SelectableTreeNode> TreeRoots { get; }

    public SourceCache<ISelectableTreeEntryViewModel, GamePath> TreeEntriesCache { get; }
}
