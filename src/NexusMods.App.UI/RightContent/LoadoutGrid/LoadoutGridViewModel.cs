using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.App.UI.Toolbars;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Type = NexusMods.App.UI.Controls.Spine.Type;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public class LoadoutGridViewModel : AViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{

    [Reactive]
    public LoadoutId Loadout { get; set; }

    private ReadOnlyObservableCollection<ModCursor> _mods;

    public ILoadoutToolbarViewModel Toolbar { get; }
    public ReadOnlyObservableCollection<ModCursor> Mods => _mods;


    private readonly SourceCache<IDataGridColumnFactory,ColumnType> _columns;
    private ReadOnlyObservableCollection<IDataGridColumnFactory> _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory>());
    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns => _filteredColumns;

    public LoadoutGridViewModel(IServiceProvider provider, LoadoutRegistry loadoutRegistry,
        IDefaultLoadoutToolbarViewModel defaultToolbar)
    {
        Toolbar = defaultToolbar;
        _columns =
            new SourceCache<IDataGridColumnFactory, ColumnType>(
                x => throw new NotImplementedException());

        _mods = new ReadOnlyObservableCollection<ModCursor>(
            new ObservableCollection<ModCursor>());

        var nameColumn = provider
            .GetRequiredService<
                DataGridColumnFactory<IModNameViewModel, ModCursor>>();
        nameColumn.Type = ColumnType.Name;
        var categoryColumn = provider.GetRequiredService<DataGridColumnFactory<IModCategoryViewModel, ModCursor>>();
        categoryColumn.Type = ColumnType.Category;
        var installedColumn = provider.GetRequiredService<DataGridColumnFactory<IModInstalledViewModel, ModCursor>>();
        installedColumn.Type = ColumnType.Installed;
        var enabledColumn = provider.GetRequiredService<DataGridColumnFactory<IModEnabledViewModel, ModCursor>>();
        enabledColumn.Type = ColumnType.Enabled;
        var versionColumn = provider.GetRequiredService<DataGridColumnFactory<IModVersionViewModel, ModCursor>>();
        versionColumn.Type = ColumnType.Version;

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
                .SelectMany(loadoutRegistry.RevisionsAsLoadouts)
                .Select(loadout => loadout!.Mods.Values.Select(m => new ModCursor(loadout.LoadoutId, m.Id)))
                .OnUI()
                .ToDiffedChangeSet(cur => cur.ModId, cur => cur)
                .Bind(out _mods)
                .Subscribe()
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Loadout)
                .BindToUi(this, vm => vm.Toolbar.LoadoutId)
                .DisposeWith(d);

            _columns.Connect()
                .Bind(out _filteredColumns)
                .Subscribe()
                .DisposeWith(d);
        });
    }
}
