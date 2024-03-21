using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.ReactiveUI;
using Humanizer.Bytes;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

public partial class ModFilesView : ReactiveUserControl<IModFilesViewModel>
{
    public ModFilesView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(view => view.ViewModel)
                    .WhereNotNull()
                    .Do(PopulateFromViewModel)
                    .Subscribe()
                    .DisposeWith(disposables);
                
                // This is a workaround for TreeDataGrid collapsing Star sized columns.
                // This forces a refresh of the width, fixing the issue.
                ModFilesTreeDataGrid.Width = double.NaN;
            }
        );
    }

    private void PopulateFromViewModel(IModFilesViewModel vm)
    {
        var source = CreateTreeSource(ViewModel!.Items);
        source.SortBy(source.Columns[0], ListSortDirection.Ascending);
        ModFilesTreeDataGrid.Source = source;
    }

    private static HierarchicalTreeDataGridSource<IFileTreeNodeViewModel> CreateTreeSource(
        ReadOnlyObservableCollection<IFileTreeNodeViewModel> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<IFileTreeNodeViewModel>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IFileTreeNodeViewModel>(
                    new TemplateColumn<IFileTreeNodeViewModel>(
                        Language.Helpers_GenerateHeader_NAME,
                        "CustomRow",
                        width: new GridLength(1, GridUnitType.Star),
                        options: new TemplateColumnOptions<IFileTreeNodeViewModel>
                        {
                            // Compares if folder first, such that folders show first, then by file name.
                            CompareAscending = (x, y) =>
                            {
                                if (x == null || y == null) return 0;
                                var folderComparison = x.IsFile.CompareTo(y.IsFile);
                                return folderComparison != 0 ? folderComparison : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                            },

                            CompareDescending = (x, y) =>
                            {
                                if (x == null || y == null) return 0;
                                var folderComparison = x.IsFile.CompareTo(y.IsFile);
                                return folderComparison != 0 ? folderComparison : string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase);
                            },
                        }
                    ),
                    node => node.Children,
                    null,
                    node => node.IsExpanded
                ),

                new TextColumn<IFileTreeNodeViewModel, string?>(
                    Language.Helpers_GenerateHeader_SIZE,
                    x => ByteSize.FromBytes(x.FileSize).ToString(),
                    options: new TextColumnOptions<IFileTreeNodeViewModel>
                    {
                        // Compares if folder first, such that folders show first, then by file name.
                        CompareAscending = (x, y) =>
                        {
                            if (x == null || y == null) return 0;
                            var folderComparison = x.IsFile.CompareTo(y.IsFile);
                            return folderComparison != 0 ? folderComparison : x.FileSize.CompareTo(y.FileSize);
                        },

                        CompareDescending = (x, y) =>
                        {
                            if (x == null || y == null) return 0;
                            var folderComparison = x.IsFile.CompareTo(y.IsFile);
                            return folderComparison != 0 ? folderComparison : y.FileSize.CompareTo(x.FileSize);
                        },
                    },
                    width: new GridLength(100)
                ),
            }
        };
    }
}
