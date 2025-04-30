using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;

internal class PreviewViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    public SourceCache<IPreviewTreeEntryViewModel, GamePath> TreeEntriesCache { get; } = new(entry => entry.GamePath);
    public ReadOnlyObservableCollection<PreviewTreeNode> TreeRoots => _treeRoots;
    private readonly ReadOnlyObservableCollection<PreviewTreeNode> _treeRoots;

    public HierarchicalTreeDataGridSource<PreviewTreeNode> Tree { get; }

    /// <summary>
    /// Constructs a new <see cref="PreviewViewModel"/>.
    /// Tree elements need to be added from the <see cref="TreeEntriesCache"/>.
    /// </summary>
    public PreviewViewModel()
    {
        TreeEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new PreviewTreeNode(node))
            .Bind(out _treeRoots)
            .Subscribe();

        Tree = TreeDataGridHelpers.CreateTreeSourceWithSingleCustomColumn<PreviewTreeNode, IPreviewTreeEntryViewModel, GamePath>(_treeRoots);
    }
}
