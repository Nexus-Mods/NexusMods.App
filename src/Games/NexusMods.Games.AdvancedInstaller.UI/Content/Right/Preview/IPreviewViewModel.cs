using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;

public interface IPreviewViewModel : IViewModelInterface
{

    public SourceCache<IPreviewTreeEntryViewModel, GamePath> TreeEntriesCache { get; }
    public ReadOnlyObservableCollection<PreviewTreeNode> TreeRoots { get; }

    public HierarchicalTreeDataGridSource<PreviewTreeNode> Tree { get; }

}
