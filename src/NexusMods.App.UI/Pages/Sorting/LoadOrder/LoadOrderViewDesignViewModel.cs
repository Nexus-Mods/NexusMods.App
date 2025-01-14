using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.App.UI.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderDesignViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public TreeDataGridAdapter<ILoadOrderItemModel, Guid> Adapter { get; set; }
    public string SortOrderName { get; set; } = "Sort Order Name";
    public string SortOrderHeading { get; set; } = "Sort Order Heading";
    public string InfoAlertTitle { get; set; } = "Info Alert Heading";
    public string InfoAlertBody { get; set; } = "Info Alert Message";
    public ReactiveCommand<Unit, Unit> InfoAlertCommand { get; } = ReactiveCommand.Create(() => { });
    public string TrophyToolTip { get; } = "Winner Tooltip";
    public ListSortDirection SortDirectionCurrent { get; set; }
    public ReactiveCommand<Unit, Unit> SwitchSortDirectionCommand { get; }
    public bool IsAscending { get; set; } = true;
    public bool IsWinnerTop { get; set; } = true;
    public string EmptyStateMessageTitle { get; } = "Empty State Message Title";
    public string EmptyStateMessageContents { get; } = "Empty State Message Contents that is long enough to wrap around and test the wrapping of the text.";
    public AlertSettingsWrapper AlertSettingsWrapper { get; }

    public LoadOrderDesignViewModel()
    {
        SwitchSortDirectionCommand = ReactiveCommand.Create(() => { IsAscending = !IsAscending; });

        Adapter = new LoadOrderTreeDataGridDesignAdapter();
        this.WhenActivated(d => { Adapter.Activate().DisposeWith(d); });

        AlertSettingsWrapper = null!;
    }
}

// adapter used for design view, based on the actual adapter LoadOrderViewModel.LoadOrderTreeDataGridAdapter 
public class LoadOrderTreeDataGridDesignAdapter : TreeDataGridAdapter<ILoadOrderItemModel, Guid>
{
    protected override IObservable<IChangeSet<ILoadOrderItemModel, Guid>> GetRootsObservable(bool viewHierarchical)
    {
        var items = new ObservableCollection<ILoadOrderItemModel>([
                new LoadOrderItemDesignModel() { DisplayName = "Item 1", Guid = Guid.NewGuid(), SortIndex = 0, IsActive = false },
                new LoadOrderItemDesignModel() { DisplayName = "Item 2", Guid = Guid.NewGuid(), SortIndex = 1, IsActive = true },
                new LoadOrderItemDesignModel() { DisplayName = "Item 3", Guid = Guid.NewGuid(), SortIndex = 2, IsActive = true },
                new LoadOrderItemDesignModel() { DisplayName = "Item 4", Guid = Guid.NewGuid(), SortIndex = 3, IsActive = false },
                new LoadOrderItemDesignModel() { DisplayName = "Item 5", Guid = Guid.NewGuid(), SortIndex = 4, IsActive = false },
                new LoadOrderItemDesignModel() { DisplayName = "Item 6", Guid = Guid.NewGuid(), SortIndex = 5, IsActive = true },
            ]
        );

        //items.Clear();

        return items.ToObservableChangeSet(item => ((LoadOrderItemDesignModel)item).Guid);
    }

    protected override IColumn<ILoadOrderItemModel>[] CreateColumns(bool viewHierarchical)
    {
        return
        [
            // TODO: Use <see cref="ColumnCreator"/> to create the columns using interfaces
            new HierarchicalExpanderColumn<ILoadOrderItemModel>(
                inner: LoadOrderTreeDataGridAdapter.CreateIndexColumn("LOAD ORDER"),
                childSelector: static model => model.Children,
                hasChildrenSelector: static model => model.HasChildren.Value,
                isExpandedSelector: static model => model.IsExpanded
            )
            {
                Tag = "expander",
            },
            LoadOrderTreeDataGridAdapter.CreateNameColumn("REDMOD NAME"),
        ];
    }
}
