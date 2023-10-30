using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

[ExcludeFromCodeCoverage]
public abstract class SelectLocationTreeBaseViewModel : AViewModel<ISelectLocationTreeViewModel>,
    ISelectLocationTreeViewModel
{
    public IAdvancedInstallerCoordinator Coordinator { get; protected set; } = null!;

    public ITreeEntryViewModel Root => _treeData ??= GetTreeData();
    private ITreeEntryViewModel? _treeData;

    /// <summary>
    /// The visual representation of the tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree => new(Root)
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

    protected abstract ITreeEntryViewModel GetTreeData();
}
