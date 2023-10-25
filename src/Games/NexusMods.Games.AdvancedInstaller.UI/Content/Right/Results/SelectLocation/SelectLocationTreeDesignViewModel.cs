using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

[ExcludeFromCodeCoverage]
public class SelectLocationTreeDesignViewModel : AViewModel<ISelectLocationTreeViewModel>, ISelectLocationTreeViewModel
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
                        new TreeEntryView
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                x => x.Children)
        }
    };


    protected virtual ITreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
    {
        var rootElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, ""),
        };

        var createFolderElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Create,
        };

        var dataElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data"),
        };

        var texturesElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data/Textures"),
        };

        var createdElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Created,
            Path = new GamePath(LocationId.Game, "Data/Textures/This is a created folder"),
        };

        var editingElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Editing,
        };

        AddChildren(rootElement, new[] { createFolderElement, dataElement });
        AddChildren(dataElement, new[] { createFolderElement, texturesElement });
        AddChildren(texturesElement, new[] { createFolderElement, createdElement, editingElement });
        return rootElement;
    }

    /// <summary>
    /// For testing and preview purposes, don't use for production.
    /// </summary>
    private static void AddChildren(TreeEntryViewModel vm, TreeEntryViewModel[] children)
    {
        foreach (var node in children)
            vm.Children.Add(node);
    }
}
