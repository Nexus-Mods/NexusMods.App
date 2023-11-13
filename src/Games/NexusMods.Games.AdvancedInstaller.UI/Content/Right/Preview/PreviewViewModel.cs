using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

internal class PreviewViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    public SourceCache<IPreviewTreeEntryViewModel, GamePath> TreeEntriesCache { get; } = new(entry => entry.GamePath);
    public ReadOnlyObservableCollection<TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>> TreeRoots => _treeRoots;
    private readonly ReadOnlyObservableCollection<TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>> _treeRoots;

    private readonly ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> _containers;
    public ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> TreeContainers => _containers;

    public PreviewViewModel()
    {
        TreeEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>(node))
            .Bind(out _treeRoots)
            .Subscribe();

        _treeRoots.ToObservableChangeSet()
            .Transform(treeNode => (ILocationPreviewTreeViewModel)new LocationPreviewTreeViewModel(treeNode))
            .Bind(out _containers)
            .Subscribe();
    }
}
