using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using DynamicData;
using NexusMods.Paths;

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

        Tree = GetTreeSource(_treeRoots);
    }

    /// <summary>
    /// Generates the required source data for the TreeDataGrid from the observable tree roots.
    /// </summary>
    /// <param name="treeRoots">Observable collection of tree roots.</param>
    /// <returns></returns>
    private static HierarchicalTreeDataGridSource<PreviewTreeNode> GetTreeSource(
        ReadOnlyObservableCollection<PreviewTreeNode> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<PreviewTreeNode>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<PreviewTreeNode>(
                    new TemplateColumn<PreviewTreeNode>(null,
                        new FuncDataTemplate<PreviewTreeNode>((node, _) =>
                            new PreviewTreeEntryView
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
}
