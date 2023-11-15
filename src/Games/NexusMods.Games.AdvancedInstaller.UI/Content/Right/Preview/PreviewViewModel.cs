using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;

internal class PreviewViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    public SourceCache<IPreviewTreeEntryViewModel, GamePath> TreeEntriesCache { get; } = new(entry => entry.GamePath);
    public ReadOnlyObservableCollection<PreviewTreeNode> TreeRoots => _treeRoots;
    private readonly ReadOnlyObservableCollection<PreviewTreeNode> _treeRoots;

    private readonly ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> _containers;
    public ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> TreeContainers => _containers;

    public PreviewViewModel()
    {
        TreeEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new PreviewTreeNode(node))
            .Bind(out _treeRoots)
            .Subscribe();

        _treeRoots.ToObservableChangeSet()
            .Transform(treeNode => (ILocationPreviewTreeViewModel)new LocationPreviewTreeViewModel(treeNode))
            .Bind(out _containers)
            .Subscribe();
    }
}
