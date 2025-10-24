using System.ComponentModel;
using Avalonia.Controls;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public partial class FileConflictsView : R3UserControl<IFileConflictsViewModel>
{
    public FileConflictsView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<FileConflictsView, IFileConflictsViewModel, CompositeItemModel<EntityId>, EntityId>(
            this, 
            FileConflictsTreeDataGrid, 
            vm => vm.TreeDataGridAdapter,
            enableDragAndDrop: true);

        this.WhenActivated(disposables =>
        {
            // TreeDataGrid Source
            this.OneWayR3Bind(view => view.BindableViewModel, vm => vm.TreeDataGridAdapter.Source, 
                    static (view, source) => view.FileConflictsTreeDataGrid.Source = source)
                .AddTo(disposables);
            
            // Empty state
            this.OneWayR3Bind(view => view.BindableViewModel, vm => vm.TreeDataGridAdapter.IsSourceEmpty, 
                    static (view, isSourceEmpty) => view.EmptyState.IsActive = isSourceEmpty)
                .AddTo(disposables);
            
            // Trophy bar
            this.OneWayR3Bind(view => view.BindableViewModel, vm => vm.SortDirectionCurrent,
                    static (view, sortDirection) =>
                    {
                        var isAscending = sortDirection == ListSortDirection.Ascending;
                        
                        DockPanel.SetDock(view.TrophyIcon, isAscending ? Dock.Bottom : Dock.Top);
                        
                        // not used anymore for styling but leaving these in just in case
                        view.TrophyBarDockPanel.Classes.ToggleIf("IsWinnerTop", !isAscending);
                        view.TrophyBarDockPanel.Classes.ToggleIf("IsWinnerBottom", isAscending);
                        
                        view.ArrowUpIcon.IsVisible = !isAscending;
                        view.ArrowDownIcon.IsVisible = isAscending;
                    })
                .AddTo(disposables);
            
            
            
        });
    }
}

