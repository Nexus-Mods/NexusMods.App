using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

using SelectableTreeNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

public class LocationTreeContainerViewModel : AViewModel<ILocationTreeContainerViewModel>, ILocationTreeContainerViewModel
{
    public LocationTreeContainerViewModel(SelectableTreeNode treeRoot)
    {
        Root = treeRoot;
        Tree = GetTreeSource(Root);
    }

    public SelectableTreeNode Root { get; }
    public HierarchicalTreeDataGridSource<SelectableTreeNode> Tree { get; }

    private static HierarchicalTreeDataGridSource<SelectableTreeNode> GetTreeSource(SelectableTreeNode root)
    {
        return new HierarchicalTreeDataGridSource<SelectableTreeNode>(root)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<SelectableTreeNode>(
                    new TemplateColumn<SelectableTreeNode>(null,
                        new FuncDataTemplate<SelectableTreeNode>((node, _) =>
                            new SelectableTreeEntryView
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
