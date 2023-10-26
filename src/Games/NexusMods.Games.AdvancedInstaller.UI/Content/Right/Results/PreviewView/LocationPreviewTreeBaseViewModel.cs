using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Paths;
using ITreeEntryViewModel =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.ITreeEntryViewModel;
using TreeEntryView = NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.TreeEntryView;
using TreeEntryViewModel =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.TreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public abstract class LocationPreviewTreeBaseViewModel : AViewModel<ILocationPreviewTreeViewModel>,
    ILocationPreviewTreeViewModel
{
    public ITreeEntryViewModel Root => TreeData ??= GetTreeData();
    protected ITreeEntryViewModel? TreeData;

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

    protected abstract ITreeEntryViewModel GetTreeData();
}
