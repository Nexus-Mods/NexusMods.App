using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public abstract class LocationPreviewTreeBaseViewModel : AViewModel<ILocationPreviewTreeViewModel>,
    ILocationPreviewTreeViewModel
{
    public IPreviewTreeEntryViewModel Root => _treeData ??= GetTreeData();
    private IPreviewTreeEntryViewModel? _treeData;

    /// <summary>
    /// The visual representation of the tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<IPreviewTreeEntryViewModel> Tree => new(Root)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<IPreviewTreeEntryViewModel>(
                new TemplateColumn<IPreviewTreeEntryViewModel>(null,
                    new FuncDataTemplate<IPreviewTreeEntryViewModel>((node, _) =>
                        new PreviewTreeEntryView
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                x => x.Children)
        }
    };

    protected abstract IPreviewTreeEntryViewModel GetTreeData();
}
