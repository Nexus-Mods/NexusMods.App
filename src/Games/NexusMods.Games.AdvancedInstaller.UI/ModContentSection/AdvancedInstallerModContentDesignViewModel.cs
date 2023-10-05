using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.App.UI;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerModContentDesignViewModel : AViewModel<IAdvancedInstallerModContentViewModel>,
    IAdvancedInstallerModContentViewModel
{
    /// <summary>
    /// The visual representation of the tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<ITreeDataGridSourceFileNode> Tree => new(GetTreeData())
    {
        Columns =
        {
            new HierarchicalExpanderColumn<ITreeDataGridSourceFileNode>(
                new TemplateColumn<ITreeDataGridSourceFileNode>("File",
                    new FuncDataTemplate<ITreeDataGridSourceFileNode>((node, scope) => new AdvancedInstallerTreeEntryView()
                    {
                        DataContext = node,
                    })),
                x => x.Children)
        }
    };

    protected virtual ITreeDataGridSourceFileNode GetTreeData() => CreateTestTree();

    private static ITreeDataGridSourceFileNode CreateTestTree()
    {
        var fileEntries = new Dictionary<RelativePath, int>
        {
            { new RelativePath("BWS.bsa"), 1 },
            { new RelativePath("BWS - Textures.bsa"), 2 },
            { new RelativePath("Readme-BWS.txt"), 3 },
            { new RelativePath("Textures/greenBlade.dds"), 4 },
            { new RelativePath("Textures/greenBlade_n.dds"), 5 },
            { new RelativePath("Textures/greenHilt.dds"), 6 },
            { new RelativePath("Textures/Armors/greenArmor.dds"), 7 },
            { new RelativePath("Textures/Armors/greenBlade.dds"), 8 },
            { new RelativePath("Textures/Armors/greenHilt.dds"), 9 },
            { new RelativePath("Meshes/greenBlade.nif"), 10 }
        };

        var tree = FileTreeNode<RelativePath, int>.CreateTree(fileEntries);
        return TreeDataGridSourceFileNode<RelativePath, int>.FromFileTree(tree);
    }
}


