using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public class SelectLocationTreeDesignViewModel : AViewModel<ISelectLocationTreeViewModel> , ISelectLocationTreeViewModel
{

    /// <summary>
    /// The visual representation of the tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree => new(GetTreeData())
    {
        Columns =
        {
            new HierarchicalExpanderColumn<ITreeEntryViewModel>(
                new TemplateColumn<ITreeEntryViewModel>(null,
                    new FuncDataTemplate<ITreeEntryViewModel>((node, scope) =>
                        new UI.TreeEntryView()
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                // TODO: Switch to AsT1
                x => x.Node.AsT2.Children)
        }
    };


    protected virtual ITreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
    {

        //var target = SelectableDirectoryNode.Create(new GamePath(LocationId.Game, ""), true);
        // TODO: Switch to using SelectableDirectoryNode
        var fakeTarget = PreviewEntryNode.Create(new GamePath(LocationId.Game, ""), true);
        return new TreeEntryViewModel(fakeTarget);
    }
}
