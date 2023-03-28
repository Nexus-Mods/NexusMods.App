using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using DynamicData;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public class LoadoutGridDesignViewModel : AViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    private readonly SourceCache<IId, ModId> _mods;
    private ReadOnlyObservableCollection<IId> _filteredMods = new(new ObservableCollection<IId>());
    public ReadOnlyObservableCollection<IId> Mods => _filteredMods;
    public LoadoutId Loadout { get; set; }

    private readonly SourceCache<DataGridColumn, ColumnType> _columns;
    private ReadOnlyObservableCollection<DataGridColumn> _filteredColumns = new(new ObservableCollection<DataGridColumn>());

    public ReadOnlyObservableCollection<DataGridColumn> Columns => _filteredColumns;

    public LoadoutGridDesignViewModel()
    {
        _mods =
            new SourceCache<IId, ModId>(
                x => throw new NotImplementedException());
        _mods.Edit(x =>
        {
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 1), ModId.From(new Guid("00000000-0000-0000-0000-000000000001")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 2), ModId.From(new Guid("00000000-0000-0000-0000-000000000002")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 3), ModId.From(new Guid("00000000-0000-0000-0000-000000000003")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 4), ModId.From(new Guid("00000000-0000-0000-0000-000000000004")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 5), ModId.From(new Guid("00000000-0000-0000-0000-000000000005")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 6), ModId.From(new Guid("00000000-0000-0000-0000-000000000006")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 7), ModId.From(new Guid("00000000-0000-0000-0000-000000000007")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 8), ModId.From(new Guid("00000000-0000-0000-0000-000000000008")));
            x.AddOrUpdate(new Id64(EntityCategory.TestData, 9), ModId.From(new Guid("00000000-0000-0000-0000-000000000009")));
        });

        _columns =
            new SourceCache<DataGridColumn, ColumnType>(
                x => throw new NotImplementedException());
        _columns.Edit(x =>
        {
            x.AddOrUpdate(new DataGridDesignViewModelColumn<IModNameViewModel, ModId>(modId => new ModNameView
            {
                ViewModel = new ModNameDesignViewModel { Row = modId }
            })
            {
                Header = "New Name"
            }, ColumnType.Name);
            x.AddOrUpdate(new DataGridTextColumn { Header = "Enabled" }, ColumnType.Enabled);
        });

        this.WhenActivated(d =>
        {
            _mods.Connect()
                .Bind(out _filteredMods)
                .Subscribe()
                .DisposeWith(d);

            _columns.Connect()
                .Bind(out _filteredColumns)
                .Subscribe()
                .DisposeWith(d);
        });
    }
}
