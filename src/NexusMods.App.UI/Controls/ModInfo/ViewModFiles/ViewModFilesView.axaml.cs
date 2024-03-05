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

namespace NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using ModFileNode = TreeNodeVM<IFileTreeNodeViewModel, GamePath>;

public partial class ViewModFilesView : ReactiveUserControl<IViewModFilesViewModel>
{
    public ViewModFilesView()
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

    private void PopulateFromViewModel(IViewModFilesViewModel vm)
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

    private static HierarchicalTreeDataGridSource<ModFileNode> CreateTreeSource(
        ReadOnlyObservableCollection<ModFileNode> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<ModFileNode>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<ModFileNode>(
                    new TemplateColumn<ModFileNode>(
                        Language.Helpers_GenerateHeader_NAME,
                        new FuncDataTemplate<ModFileNode>((node, _) =>
                            {
                                // This should never be null but can be during rapid resize, due to
                                // virtualization shenanigans. Think this is a control bug, but well, gotta work with what we have.
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                                if (node == null)
                                    return new Control();
                                    
                                // Very sus but it works, t r u s t.
                                var view = new FileTreeNodeView();
                                view!.DataContext = node.Item;
                                
                                // This is a 'hack' which allows us to receive events from the wrapping 'ModFileNode'
                                // and transfer it into the child IFileTreeNodeViewModel.
                                
                                // Given that we may reuse the `FileTreeNodeView` for other views in the future, 
                                // which may not necessarily need the wrapping `ModFileNode` to get info on when it's
                                // expanded or not expanded, this does not seem unreasonable to do.
                                ((IActivatableViewModel)node.Item).WhenActivated(d =>
                                {
                                    node.WhenAnyValue(x => x.IsExpanded)
                                        .Subscribe(isExpanded => node.Item.OnExpanded(isExpanded))
                                        .DisposeWith(d);
                                });
                                
                                return view;
                            }
                        ),
                        width: new GridLength(1, GridUnitType.Star),
                        options: new()
                        {
                            // Compares if folder first, such that folders show first, then by file name.
                            CompareAscending = (x, y) => 
                            {
                                var folderComparison = x!.Item.IsFile.CompareTo(y!.Item.IsFile); 
                                return folderComparison != 0 ? folderComparison : string.Compare(x!.Item.Name, y!.Item.Name, StringComparison.OrdinalIgnoreCase);
                            },

                            CompareDescending = (x, y) => 
                            {
                                var folderComparison = x!.Item.IsFile.CompareTo(y!.Item.IsFile); 
                                return folderComparison != 0 ? folderComparison : string.Compare(y!.Item.Name, x!.Item.Name, StringComparison.OrdinalIgnoreCase);
                            },
                        }
                    ),
                    node => node.Children,
                    null,
                    node => node.IsExpanded),
                
                new TextColumn<ModFileNode,string?>(
                    Language.Helpers_GenerateHeader_SIZE,
                    x => ByteSize.FromBytes(x.Item.FileSize).ToString(),
                    options: new()
                    {
                        // Compares if folder first, such that folders show first, then by file name.
                        CompareAscending = (x, y) => 
                        {
                            var folderComparison = x!.Item.IsFile.CompareTo(y!.Item.IsFile);
                            return folderComparison != 0 ? folderComparison : x!.Item.FileSize.CompareTo(y!.Item.FileSize);
                        },

                        CompareDescending = (x, y) => 
                        {
                            var folderComparison = x!.Item.IsFile.CompareTo(y!.Item.IsFile);  
                            return folderComparison != 0 ? folderComparison : y!.Item.FileSize.CompareTo(x!.Item.FileSize);
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

