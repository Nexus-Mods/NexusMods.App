using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using DynamicData;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.App.UI.Toolbars;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public class LoadoutGridDesignViewModel : AViewModel<ILoadoutGridViewModel>,
    ILoadoutGridViewModel
{
    private readonly SourceCache<ModCursor, ModId> _mods;

    private ReadOnlyObservableCollection<ModCursor> _filteredMods =
        new(new ObservableCollection<ModCursor>());

    public ILoadoutToolbarViewModel Toolbar =>
        new DefaultLoadoutToolbarDesignViewModel();
    public ReadOnlyObservableCollection<ModCursor> Mods => _filteredMods;
    public LoadoutId Loadout { get; set; } = Initializers.LoadoutId;

    private readonly SourceCache<IDataGridColumnFactory, ColumnType> _columns;

    private ReadOnlyObservableCollection<IDataGridColumnFactory>
        _filteredColumns =
            new(new ObservableCollection<IDataGridColumnFactory>());

    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns =>
        _filteredColumns;

    public LoadoutGridDesignViewModel()
    {
        _mods =
            new SourceCache<ModCursor, ModId>(
                x => x.ModId);
        _mods.Edit(x =>
        {
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000001"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000002"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000003"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000004"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000005"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000006"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000007"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000008"))));
            x.AddOrUpdate(new ModCursor(Loadout,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000009"))));
        });

        _columns =
            new SourceCache<IDataGridColumnFactory, ColumnType>(
                x => throw new NotImplementedException());
        _columns.Edit(x =>
        {
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModNameViewModel, ModCursor>(
                    modId => new ModNameView
                    {
                        ViewModel = new ModNameDesignViewModel { Row = modId }
                    }, ColumnType.Name));
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModVersionViewModel,
                    ModCursor>(modId => new ModVersionView()
                {
                    ViewModel = new ModVersionDesignViewModel() { Row = modId }
                }, ColumnType.Version));
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModCategoryViewModel,
                    ModCursor>(modId => new ModCategoryView
                {
                    ViewModel = new ModCategoryDesignViewModel() { Row = modId }
                }, ColumnType.Category));
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModInstalledViewModel,
                    ModCursor>(modId => new ModInstalledView
                {
                    ViewModel = new ModInstalledDesignViewModel { Row = modId, Status = ModStatus.Failed}
                }, ColumnType.Installed));
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModEnabledViewModel,
                    ModCursor>(modId => new ModEnabledView
                {
                    ViewModel = new ModEnabledDesignViewModel { Row = modId, Status = ModStatus.Installing}
                }, ColumnType.Enabled));
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
