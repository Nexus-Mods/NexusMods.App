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
                x => x.Node.AsT1.Children)
        }
    };


    protected virtual ITreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
    {

        var RootElement = new SelectableDirectoryNode
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, ""),
        };

        var createFolderElement = new SelectableDirectoryNode()
        {
            Status = SelectableDirectoryNodeStatus.Create,
        };

        var dataElement = new SelectableDirectoryNode
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data"),
        };

        var texturesElement = new SelectableDirectoryNode
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data/Textures"),
        };

        var createdElement = new SelectableDirectoryNode()
        {
            Status = SelectableDirectoryNodeStatus.Created,
            Path = new GamePath(LocationId.Game, "Data/Textures/This is a created folder"),
        };

        var editingElement = new SelectableDirectoryNode()
        {
            Status = SelectableDirectoryNodeStatus.Editing,
        };


        RootElement.AddChildren(new []{createFolderElement, dataElement});
        dataElement.AddChildren(new[]{createFolderElement, texturesElement});
        texturesElement.AddChildren(new[]{createFolderElement, createdElement, editingElement});

        return new TreeEntryViewModel(RootElement);
    }
}
