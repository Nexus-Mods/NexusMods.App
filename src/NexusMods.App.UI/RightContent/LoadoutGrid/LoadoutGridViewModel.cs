using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using Noggog;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public class LoadoutGridViewModel : AViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{

    public LoadoutId Loadout { get; set; }

    private ReadOnlyObservableCollection<ModCursor> _mods;
    public ReadOnlyObservableCollection<ModCursor> Mods => _mods;


    private readonly SourceCache<DataGridColumn,ColumnType> _columns;
    private ReadOnlyObservableCollection<DataGridColumn> _filteredColumns = new(new ObservableCollection<DataGridColumn>());
    public ReadOnlyObservableCollection<DataGridColumn> Columns => _filteredColumns;

    public LoadoutGridViewModel(IServiceProvider provider, LoadoutRegistry loadoutRegistry,
        IModNameViewModel nameViewModel, IModCategoryViewModel categoryViewModel,
        IModInstalledViewModel modInstalledViewModel, IModEnabledViewModel modEnabledViewModel,
        IModVersionViewModel versionViewModel)
    {
        _columns =
            new SourceCache<DataGridColumn, ColumnType>(
                x => throw new NotImplementedException());

        _mods = new ReadOnlyObservableCollection<ModCursor>(new ObservableCollection<ModCursor>());

        var nameColumn = provider.GetRequiredService<DataGridViewModelColumn<IModNameViewModel, ModCursor>>();
        nameColumn.Header = "MOD NAME";

        var categoryColumn = provider.GetRequiredService<DataGridViewModelColumn<IModCategoryViewModel, ModCursor>>();
        categoryColumn.Header = "CATEGORY";

        var installedColumn = provider.GetRequiredService<DataGridViewModelColumn<IModInstalledViewModel, ModCursor>>();
        installedColumn.Header = "INSTALLED";

        var enabledColumn = provider.GetRequiredService<DataGridViewModelColumn<IModEnabledViewModel, ModCursor>>();
        enabledColumn.Header = "ENABLED";

        var versionColumn = provider.GetRequiredService<DataGridViewModelColumn<IModVersionViewModel, ModCursor>>();
        versionColumn.Header = "VERSION";

        _columns.Edit(x =>
        {
            x.AddOrUpdate(nameColumn, ColumnType.Name);
            x.AddOrUpdate(versionColumn, ColumnType.Version);
            x.AddOrUpdate(categoryColumn, ColumnType.Category);
            x.AddOrUpdate(installedColumn, ColumnType.Installed);
            x.AddOrUpdate(enabledColumn, ColumnType.Enabled);
        });

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Loadout)
                .Select(loadoutRegistry.Get)
                .Where(loadout => loadout != null)
                .Select(loadout => loadout!.Mods.Keys.Select(modId =>
                    new ModCursor(loadout.LoadoutId, modId)))
                .ToObservableChangeSet(cursor => cursor.ModId)
                .Bind(out _mods)
                .Subscribe()
                .DisposeWith(d);

            _columns.Connect()
                .Bind(out _filteredColumns)
                .Subscribe()
                .DisposeWith(d);
        });
    }
}
