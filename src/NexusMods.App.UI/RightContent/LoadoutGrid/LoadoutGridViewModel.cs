using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public class LoadoutGridViewModel : AViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{

    [Reactive]
    public LoadoutId Loadout { get; set; }

    private ReadOnlyObservableCollection<ModCursor> _mods;
    public ReadOnlyObservableCollection<ModCursor> Mods => _mods;


    private readonly SourceCache<IDataGridColumnFactory,ColumnType> _columns;
    private ReadOnlyObservableCollection<IDataGridColumnFactory> _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory>());
    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns => _filteredColumns;

    public LoadoutGridViewModel(IServiceProvider provider, LoadoutRegistry loadoutRegistry,
        IModNameViewModel nameViewModel, IModCategoryViewModel categoryViewModel,
        IModInstalledViewModel modInstalledViewModel, IModEnabledViewModel modEnabledViewModel,
        IModVersionViewModel versionViewModel)
    {
        _columns =
            new SourceCache<IDataGridColumnFactory, ColumnType>(
                x => throw new NotImplementedException());

        _mods = new ReadOnlyObservableCollection<ModCursor>(
            new ObservableCollection<ModCursor>());

        var nameColumn = provider.GetRequiredService<DataGridColumnFactory<IModNameViewModel, ModCursor>>();
        nameColumn.Header = "MOD NAME";

        var categoryColumn = provider.GetRequiredService<DataGridColumnFactory<IModCategoryViewModel, ModCursor>>();
        categoryColumn.Header = "CATEGORY";

        var installedColumn = provider.GetRequiredService<DataGridColumnFactory<IModInstalledViewModel, ModCursor>>();
        installedColumn.Header = "INSTALLED";

        var enabledColumn = provider.GetRequiredService<DataGridColumnFactory<IModEnabledViewModel, ModCursor>>();
        enabledColumn.Header = "ENABLED";

        var versionColumn = provider.GetRequiredService<DataGridColumnFactory<IModVersionViewModel, ModCursor>>();
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
                .Select(loadout => loadout!.Mods.Values.Select(m => new ModCursor(loadout.LoadoutId, m.Id)))
                .ToDiffedChangeSet(cur => cur.ModId, cur => cur)
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
