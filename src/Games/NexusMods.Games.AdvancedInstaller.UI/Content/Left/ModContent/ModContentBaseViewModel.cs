using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal abstract class ModContentBaseViewModel : AViewModel<IModContentViewModel>,
    IModContentViewModel
{
    public IObserver<IModContentTreeEntryViewModel> StartSelectObserver { get; protected set; } = null!;
    public IObserver<IModContentTreeEntryViewModel> CancelSelectObserver { get; protected set; } = null!;

    public IModContentTreeEntryViewModel Root => _treeData ??= InitTreeData();
    private IModContentTreeEntryViewModel? _treeData;

    /// <summary>
    /// The visual representation of the tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<IModContentTreeEntryViewModel> Tree => new(Root)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<IModContentTreeEntryViewModel>(
                new TemplateColumn<IModContentTreeEntryViewModel>(null,
                    new FuncDataTemplate<IModContentTreeEntryViewModel>((node, _) =>
                        new ModContentTreeEntryView
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                x => x.Children)
        }
    };

    protected abstract IModContentTreeEntryViewModel InitTreeData();
}
