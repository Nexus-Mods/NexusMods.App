using System.ComponentModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public partial class LoadOrderView : ReactiveUserControl<ILoadOrderViewModel>
{
    public LoadOrderView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                TreeDataGridViewHelper.SetupTreeDataGridAdapter<LoadOrderView, ILoadOrderViewModel, CompositeItemModel<Guid>, Guid>(
                    this,
                    SortOrderTreeDataGrid,
                    vm => vm.Adapter
                );

                // TreeDataGrid Source
                this.OneWayBind(ViewModel,
                        vm => vm.Adapter.Source.Value,
                        view => view.SortOrderTreeDataGrid.Source
                    )
                    .DisposeWith(disposables);

                // Trophy bar
                this.WhenAnyValue(view => view.ViewModel!.IsWinnerTop)
                    .Subscribe(isWinnerTop =>
                        {
                            DockPanel.SetDock(TrophyIcon, isWinnerTop ? Dock.Top : Dock.Bottom);
                            TrophyBarPanel.Classes.ToggleIf("IsWinnerTop", isWinnerTop);
                            TrophyBarPanel.Classes.ToggleIf("IsWinnerBottom", !isWinnerTop);
                        }
                    )
                    .DisposeWith(disposables);

                // Trophy bar arrow
                this.WhenAnyValue(view => view.ViewModel!.SortDirectionCurrent)
                    .Subscribe(sortCurrentDirection =>
                        {
                            var isAscending = sortCurrentDirection == ListSortDirection.Ascending;
                            ArrowUpIcon.IsVisible = !isAscending;
                            ArrowDownIcon.IsVisible = isAscending;
                        }
                    )
                    .DisposeWith(disposables);

                // trophy tooltip
                this.WhenAnyValue(view => view.ViewModel!.TrophyToolTip)
                    .Subscribe(tooltip => { ToolTip.SetTip(TrophyBarPanel, tooltip); })
                    .DisposeWith(disposables);

                // Empty state
                this.OneWayBind(ViewModel,
                        vm => vm.Adapter.IsSourceEmpty.Value,
                        view => view.EmptyState.IsActive
                    )
                    .DisposeWith(disposables);

                // Empty state Header
                this.OneWayBind(ViewModel,
                        vm => vm.EmptyStateMessageTitle,
                        view => view.EmptyState.Header
                    )
                    .DisposeWith(disposables);

                // Empty state Message
                this.OneWayBind(ViewModel,
                        vm => vm.EmptyStateMessageContents,
                        view => view.EmptySpaceMessageTextBlock.Text
                    )
                    .DisposeWith(disposables);

                // Title
                this.OneWayBind(ViewModel, vm => vm.SortOrderHeading,
                        view => view.TitleTextBlock.Text
                    )
                    .DisposeWith(disposables);

                // alert title
                this.OneWayBind(ViewModel,
                        vm => vm.InfoAlertTitle,
                        view => view.LoadOrderAlert.Title
                    )
                    .DisposeWith(disposables);

                // alert body
                this.OneWayBind(ViewModel,
                        vm => vm.InfoAlertBody,
                        view => view.LoadOrderAlert.Body
                    )
                    .DisposeWith(disposables);

                // Alert settings
                this.OneWayBind(ViewModel, vm => vm.AlertSettingsWrapper,
                        view => view.LoadOrderAlert.AlertSettings
                    )
                    .DisposeWith(disposables);

                // Alert Command
                this.OneWayBind(ViewModel, vm => vm.InfoAlertCommand,
                        view => view.InfoAlertButton.Command
                    )
                    .DisposeWith(disposables);
            }
        );
    }

    private void OnRowDrop(object? sender, TreeDataGridRowDragEventArgs e)
    {
        
        // NOTE(Al12rs): This is important in case the source is read-only, otherwise TreeDataGrid will attempt to
        // move the items, updating the source collection, throwing an exception in the process.
        e.Handled = true;

        var dataObject = e.Inner.Data as DataObject;
        var a = dataObject?.Get("TreeDataGridDragInfo");
        var dragInfo = a as DragInfo;
        if (dragInfo is null) return;

        var source = dragInfo.Source;
        var indices = dragInfo.Indexes;

        var models = new List<LoadOrderItemModel>();

        foreach (var modelIndex in indices)
        {
            var rowIndex = source.Rows.ModelIndexToRowIndex(modelIndex);
            var row = source.Rows[rowIndex];
            var model = row.Model;
            if (model is LoadOrderItemModel loadOrderItemModel)
                models.Add(loadOrderItemModel);
        }
    }
}
