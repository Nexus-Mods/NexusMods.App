using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;
using ITreeEntryViewModel =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.ITreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public class LocationPreviewTreeDesignViewModel : AViewModel<ILocationPreviewTreeViewModel>,
    ILocationPreviewTreeViewModel
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
                        new TreeEntryView()
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                x => x.Children)
        }
    };

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual ITreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
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

        var target = TreeEntryViewModel.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in fileEntries)
            target.AddChildren(file, false);

        return target;
    }
}
