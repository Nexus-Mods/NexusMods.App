using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal abstract class ModContentBaseViewModel : AViewModel<IModContentViewModel>,
    IModContentViewModel
{
    public IObserver<ITreeEntryViewModel> StartSelectObserver { get; protected set; } = null!;
    public IObserver<ITreeEntryViewModel> CancelSelectObserver { get; protected set; } = null!;

    public ITreeEntryViewModel Root => _treeData ??= InitTreeData();
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
                    new FuncDataTemplate<ITreeEntryViewModel>((node, _) =>
                        new TreeEntryView
                        {
                            DataContext = node,
                        }),
                    width: new GridLength(1, GridUnitType.Star)
                ),
                x => x.Children)
        }
    };

    protected abstract ITreeEntryViewModel InitTreeData();
}
