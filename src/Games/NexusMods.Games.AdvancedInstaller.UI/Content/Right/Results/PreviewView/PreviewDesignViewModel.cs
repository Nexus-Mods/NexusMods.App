using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

internal class PreviewDesignViewModel : PreviewViewModel
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
                x => x.Node.AsT0.Children)
        }
    };

    protected virtual ITreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
    {
        var fileEntries = new RelativePath[]
        {
            new RelativePath("BWS.bsa"),
            new RelativePath("BWS - Textures.bsa"),
            new RelativePath("Readme-BWS.txt"),
            new RelativePath("Textures/greenBlade.dds"),
            new RelativePath("Textures/greenBlade_n.dds"),
            new RelativePath("Textures/greenHilt.dds"),
            new RelativePath("Textures/Armors/greenArmor.dds"),
            new RelativePath("Textures/Armors/greenBlade.dds"),
            new RelativePath("Textures/Armors/greenHilt.dds"),
            new RelativePath("Meshes/greenBlade.nif")
        };

        var target = PreviewEntryNode.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in fileEntries)
            target.AddChild(file, false);

        return new TreeEntryViewModel(target);
    }
}
