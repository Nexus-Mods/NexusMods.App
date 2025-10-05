using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Alerts;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderDesignViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public TreeDataGridAdapter<CompositeItemModel<ISortItemKey>, ISortItemKey> Adapter { get; set; }
    public string SortOrderName { get; set; } = "Sort Order Name";
    public string InfoAlertTitle { get; set; } = "Info Alert Heading";
    public string InfoAlertBody { get; set; } = "Info Alert Message";
    public ReactiveCommand<Unit, Unit> ToggleAlertCommand { get; } = ReactiveCommand.Create(() => { });

    public ReactiveCommand<Unit, Unit> LearnMoreAlertCommand { get; } = ReactiveCommand.Create(() => { });
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
public class LoadOrderTreeDataGridDesignAdapter : TreeDataGridAdapter<CompositeItemModel<ISortItemKey>, ISortItemKey>
{
    protected override IObservable<IChangeSet<CompositeItemModel<ISortItemKey>, ISortItemKey>> GetRootsObservable(bool viewHierarchical)
    {
        var items = new ObservableCollection<CompositeItemModel<ISortItemKey>>([
                CreateDesignModel("Item 0", Guid.NewGuid(), 0, true),
                CreateDesignModel("Item 1", Guid.NewGuid(), 1, false),
                CreateDesignModel("Item 2", Guid.NewGuid(), 2, true),
                CreateDesignModel("Item 3", Guid.NewGuid(), 3, false),
                CreateDesignModel("Item 4", Guid.NewGuid(), 4, true),
                CreateDesignModel("Item 5", Guid.NewGuid(), 5, false),
                CreateDesignModel("Item 6", Guid.NewGuid(), 6, true),
            ]
        );

        return items.ToObservableChangeSet(item => ((item).Key));
    }

    protected override IColumn<CompositeItemModel<ISortItemKey>>[] CreateColumns(bool viewHierarchical)
    {
        var indexColumn = ColumnCreator.Create<ISortItemKey, LoadOrderColumns.IndexColumn>(
            columnHeader: "Load Order",
            canUserSortColumn: false,
            canUserResizeColumn: false
        );
        
        var expanderColumn = ITreeDataGridItemModel<CompositeItemModel<ISortItemKey>, ISortItemKey>.CreateExpanderColumn(indexColumn);

        return
        [
            expanderColumn,
            ColumnCreator.Create<ISortItemKey, LoadOrderColumns.DisplayNameColumn>(
                columnHeader: "DisplayName",
                canUserSortColumn: false,
                canUserResizeColumn: false
            ),
            ColumnCreator.Create<ISortItemKey, LoadOrderColumns.ModNameColumn>(
                columnHeader: "ModName",
                canUserSortColumn: false,
                canUserResizeColumn: false
            ),
        ];
    }

    private CompositeItemModel<ISortItemKey> CreateDesignModel(string name, Guid guid, int sortIndex, bool isActive)
    {
        var model = new CompositeItemModel<ISortItemKey>(new SortItemKey<Guid>(guid));
        model.Add(LoadOrderColumns.DisplayNameColumn.DisplayNameComponentKey, new StringComponent(name));
        model.Add(LoadOrderColumns.ModNameColumn.ModNameComponentKey, new StringComponent(name));
        model.Add(LoadOrderColumns.IsActiveComponentKey, new ValueComponent<bool>(isActive));

        model.Add(LoadOrderColumns.IndexColumn.IndexComponentKey,
            new LoadOrderComponents.IndexComponent(
                new ValueComponent<int>(sortIndex),
                new ValueComponent<string>(sortIndex.Ordinalize()),
                R3.Observable.Return(true),
                R3.Observable.Return(true)
            )
        );

        return model;
    }
    
}
