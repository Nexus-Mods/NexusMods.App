using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;

public class LocationPreviewTreeViewModel : AViewModel<ILocationPreviewTreeViewModel>, ILocationPreviewTreeViewModel
{
    public PreviewTreeNode Root { get; }
    public HierarchicalTreeDataGridSource<PreviewTreeNode> Tree { get; }

    public LocationPreviewTreeViewModel(PreviewTreeNode treeRoot)
    {
        Root = treeRoot;
        Tree = GetTreeSource(Root);
    }

    protected static HierarchicalTreeDataGridSource<PreviewTreeNode> GetTreeSource(PreviewTreeNode root)
    {
        return new HierarchicalTreeDataGridSource<PreviewTreeNode>(root)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<PreviewTreeNode>(
                    new TemplateColumn<PreviewTreeNode>(null,
                        new FuncDataTemplate<PreviewTreeNode>((node, _) =>
                            new PreviewTreeEntryView
                            {
                                DataContext = node?.Item,
                            }),
                        width: new GridLength(1, GridUnitType.Star)
                    ),
                    x => x.Children)
            }
        };
    }
}
