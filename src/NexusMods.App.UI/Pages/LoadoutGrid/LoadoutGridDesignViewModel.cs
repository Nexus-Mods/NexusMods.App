using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ModCategoryView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory.ModCategoryView;
using ModEnabledView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled.ModEnabledView;
using ModInstalledView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled.ModInstalledView;
using ModNameView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName.ModNameView;
using ModVersionView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion.ModVersionView;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

public class LoadoutGridDesignViewModel : APageViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    private readonly SourceCache<ModCursor, ModId> _mods;

    private ReadOnlyObservableCollection<ModCursor> _filteredMods =
        new(new ObservableCollection<ModCursor>());

    public ReadOnlyObservableCollection<ModCursor> Mods => _filteredMods;

    public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    private readonly SourceCache<IDataGridColumnFactory<LoadoutColumn>, LoadoutColumn> _columns;

    private ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>>
        _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory<LoadoutColumn>>());

    public string LoadoutName => "My Test Loadout";

    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns => _filteredColumns;
    public int SelectedIndex { get; set; }
    public ModCursor[] SelectedItems { get; set; } = Array.Empty<ModCursor>();

    public ReactiveCommand<NavigationInformation, Unit> ViewModContentsCommand { get; } = ReactiveCommand.Create<NavigationInformation>(_ => { });

    public Task AddMod(string path)
    {
        _mods.Edit(x => { x.AddOrUpdate(new ModCursor(LoadoutId, ModId.From(Guid.NewGuid()))); });
        return Task.CompletedTask;
    }

    public Task AddModAdvanced(string path)
    {
        _mods.Edit(x => { x.AddOrUpdate(new ModCursor(LoadoutId, ModId.From(Guid.NewGuid()))); });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Used for unit tests
    /// </summary>
    /// <param name="cursor">Row to add</param>
    /// <returns></returns>
    public void AddMod(ModCursor cursor)
    {
        _mods.Edit(x =>
        {
            x.AddOrUpdate(cursor);
        });
    }

    public Task DeleteMods(IEnumerable<ModId> modsToDelete, string commitMessage)
    {
        _mods.Edit(x =>
        {
            foreach (var mod in modsToDelete)
            {
                x.Remove(mod);
            }
        });
        return Task.CompletedTask;
    }

    public LoadoutGridDesignViewModel() : base(DesignWindowManager.Instance)
    {
        _mods = new SourceCache<ModCursor, ModId>(x => x.ModId);
        _mods.Edit(x =>
        {
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000001"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000002"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000003"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000004"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000005"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000006"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000007"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000008"))));
            x.AddOrUpdate(new ModCursor(LoadoutId,
                ModId.From(new Guid("00000000-0000-0000-0000-000000000009"))));
        });

        _columns =
            new SourceCache<IDataGridColumnFactory<LoadoutColumn>, LoadoutColumn>(
                x => x.Type);
        _columns.Edit(x =>
        {
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModNameViewModel, ModCursor, LoadoutColumn>(
                    modId => new ModNameView
                    {
                        ViewModel = new ModNameDesignViewModel { Row = modId }
                    }, LoadoutColumn.Name)
                {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModVersionViewModel,
                    ModCursor, LoadoutColumn>(modId => new ModVersionView
                {
                    ViewModel = new ModVersionDesignViewModel { Row = modId }
                }, LoadoutColumn.Version));
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModCategoryViewModel,
                    ModCursor, LoadoutColumn>(modId => new ModCategoryView
                {
                    ViewModel = new ModCategoryDesignViewModel { Row = modId }
                }, LoadoutColumn.Category));
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModInstalledViewModel,
                    ModCursor, LoadoutColumn>(modId => new ModInstalledView
                {
                    ViewModel = new ModInstalledDesignViewModel { Row = modId, Status = ModStatus.Failed }
                }, LoadoutColumn.Installed));
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IModEnabledViewModel,
                    ModCursor, LoadoutColumn>(modId => new ModEnabledView
                {
                    ViewModel = new ModEnabledDesignViewModel { Row = modId, Status = ModStatus.Installing }
                }, LoadoutColumn.Enabled));
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
