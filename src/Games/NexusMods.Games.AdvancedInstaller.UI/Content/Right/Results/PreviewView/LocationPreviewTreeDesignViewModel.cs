using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public class LocationPreviewTreeDesignViewModel : AViewModel<ILocationPreviewTreeViewModel>,
    ILocationPreviewTreeViewModel
{
    /// <summary>
    /// The visual representation of the tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<IPreviewTreeEntryViewModel> Tree => new(GetTreeData())
    {
        Columns =
        {
            new HierarchicalExpanderColumn<IPreviewTreeEntryViewModel>(
                new TemplateColumn<IPreviewTreeEntryViewModel>(null,
                    new FuncDataTemplate<IPreviewTreeEntryViewModel>((node, scope) =>
                        new SelectableDirectoryEntryView()
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                x => x.Node.AsT2.Children)
        }
    };

    protected virtual IPreviewTreeEntryViewModel GetTreeData() => CreateTestTree();

    private static IPreviewTreeEntryViewModel CreateTestTree()
    {
        var fileEntries = new RelativePath[]
        {
            new("BWS.bsa"),
            new("BWS - Textures.bsa"),
            new("Readme-BWS.txt"),
            new("Textures/greenBlade.dds"),
            new("Textures/greenBlade_n.dds"),
            new("Textures/greenHilt.dds"),
            new("Textures/Armors/greenArmor.dds"),
            new("Textures/Armors/greenBlade.dds"),
            new("Textures/Armors/greenHilt.dds"),
            new("Meshes/greenBlade.nif")
        };

        var target = PreviewEntryNode.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in fileEntries)
            target.AddChildren(file, false);

        return new PreviewTreeEntryViewModel(target);
    }
}
