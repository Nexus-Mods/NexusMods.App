using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.ReactiveUI;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
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
        });
    }

    private void PopulateFromViewModel(IModFilesViewModel vm)
    {
        var source = CreateTreeSource(ViewModel!.Items);
        source.SortBy(source.Columns[0], ListSortDirection.Ascending);
        ModFilesTreeDataGrid.Source = source;
            
        // Hide Stack Panel
        MultiLocationStackPanel.IsVisible = ViewModel.RootCount > 1;
        SingleLocationStackPanel.IsVisible = ViewModel.RootCount <= 1;

        SingleLocationText.Text = ViewModel.PrimaryRootLocation;
        MultiLocationCountText.Text = $"{ViewModel.RootCount}";
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
                        new FuncDataTemplate<IFileTreeNodeViewModel>((node, _) =>
                            {
                                // This should never be null but can be during rapid resize, due to
                                // virtualization shenanigans. Think this is a control bug, but well, gotta work with what we have.
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                                if (node == null)
                                    return new Control();

                                var view = new FileTreeNodeView();
                                view.DataContext = node;
                                return view;
                            }
                        ),
                        width: new GridLength(1, GridUnitType.Star),
                        options: new()
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
                    node => node.IsExpanded),
                
                new TextColumn<IFileTreeNodeViewModel,string?>(
                    Language.Helpers_GenerateHeader_SIZE,
                    x => ByteSize.FromBytes(x.FileSize).ToString(),
                    options: new()
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
                    // HACK(sewer): If I don't overwrite this, the column may be zero sized if put into certain containers, e.g. StackPanel
                    //              Since I need a min size anyway, this isn't a bad way to set it.
                    width:new GridLength(100)
                ),
            }
        };
    }
}

