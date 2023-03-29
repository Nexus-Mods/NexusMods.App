using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using DynamicData;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public class LoadoutGridDesignViewModel : AViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    private readonly SourceCache<ModCursor, ModId> _mods;
    private ReadOnlyObservableCollection<ModCursor> _filteredMods = new(new ObservableCollection<ModCursor>());
    public ReadOnlyObservableCollection<ModCursor> Mods => _filteredMods;
    public LoadoutId Loadout { get; set; } = Initializers.LoadoutId;

    private readonly SourceCache<DataGridColumn, ColumnType> _columns;
    private ReadOnlyObservableCollection<DataGridColumn> _filteredColumns = new(new ObservableCollection<DataGridColumn>());

    public ReadOnlyObservableCollection<DataGridColumn> Columns => _filteredColumns;

    public LoadoutGridDesignViewModel()
    {
        _mods =
            new SourceCache<ModCursor, ModId>(
                x => x.ModId);
        _mods.Edit(x =>
        {
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000001"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000002"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000003"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000004"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000005"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000006"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000007"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000008"))));
            x.AddOrUpdate(new ModCursor(Loadout, ModId.From(new Guid("00000000-0000-0000-0000-000000000009"))));
        });

        _columns =
            new SourceCache<DataGridColumn, ColumnType>(
                x => throw new NotImplementedException());
        _columns.Edit(x =>
        {
            x.AddOrUpdate(new DataGridDesignViewModelColumn<IModNameViewModel, ModCursor>(modId => new ModNameView
            {
                ViewModel = new ModNameDesignViewModel { Row = modId }
            })
            {
                Header = "New Name"
            }, ColumnType.Name);
            x.AddOrUpdate(new DataGridDesignViewModelColumn<IModVersionViewModel, ModCursor>(modId => new ModVersionView()
            {
                ViewModel = new ModVersionDesignViewModel() { Row = modId }
            })
            {
                Header = "Version"
            }, ColumnType.Version);
            x.AddOrUpdate(new DataGridDesignViewModelColumn<IModInstalledViewModel, ModCursor>(modId => new ModInstalledView
            {
                ViewModel = new ModInstalledDesignViewModel { Row = modId }
            })
            {
                Header = "Installed"
            }, ColumnType.Installed);
            x.AddOrUpdate(new DataGridDesignViewModelColumn<IModEnabledViewModel, ModCursor>(modId => new ModEnabledView
            {
                ViewModel = new ModEnabledDesignViewModel { Row = modId }
            })
            {
                Header = "New Enabled"
            }, ColumnType.Enabled);
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
