using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;


namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public abstract class SelectLocationTreeBaseViewModel : AViewModel<ISelectLocationTreeViewModel>,
    ISelectLocationTreeViewModel
{
    public IAdvancedInstallerCoordinator Coordinator { get; protected init; } = null!;

    public ISelectableTreeEntryViewModel Root => _treeData ??= GetTreeData();
    private ISelectableTreeEntryViewModel? _treeData;

    /// <summary>
    /// The visual representation of the tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<ISelectableTreeEntryViewModel> Tree => new(Root)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<ISelectableTreeEntryViewModel>(
                new TemplateColumn<ISelectableTreeEntryViewModel>(null,
                    new FuncDataTemplate<ISelectableTreeEntryViewModel>((node, _) =>
                        new SelectableTreeEntryView
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                x => x.Children)
        }
    };

    protected abstract ISelectableTreeEntryViewModel GetTreeData();
}
