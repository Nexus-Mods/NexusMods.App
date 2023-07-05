using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public class LoadoutGridViewModel : AViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    [Reactive]
    public LoadoutId LoadoutId { get; set; }

    private ReadOnlyObservableCollection<ModCursor> _mods;
    public ReadOnlyObservableCollection<ModCursor> Mods => _mods;


    private readonly SourceCache<IDataGridColumnFactory,ColumnType> _columns;
    private ReadOnlyObservableCollection<IDataGridColumnFactory> _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory>());
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LoadoutGridViewModel> _logger;
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly IArchiveInstaller _archiveInstaller;

    [Reactive]
    public string LoadoutName { get; set; } = "";
    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns => _filteredColumns;


    public LoadoutGridViewModel(
        ILogger<LoadoutGridViewModel> logger,
        IServiceProvider provider,
        LoadoutRegistry loadoutRegistry,
        IFileSystem fileSystem,
        IArchiveAnalyzer archiveAnalyzer,
        IArchiveInstaller archiveInstaller)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _loadoutRegistry = loadoutRegistry;
        _archiveAnalyzer = archiveAnalyzer;
        _archiveInstaller = archiveInstaller;

        _columns =
            new SourceCache<IDataGridColumnFactory, ColumnType>(
                x => throw new NotImplementedException());

        _mods = new ReadOnlyObservableCollection<ModCursor>(
            new ObservableCollection<ModCursor>());

        var nameColumn = provider
            .GetRequiredService<
                DataGridColumnFactory<IModNameViewModel, ModCursor>>();
        nameColumn.Type = ColumnType.Name;
        nameColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
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
            this.WhenAnyValue(vm => vm.LoadoutId)
                .SelectMany(loadoutRegistry.RevisionsAsLoadouts)
                .Select(loadout => loadout!.Mods.Values.Select(m => new ModCursor(loadout.LoadoutId, m.Id)))
                .OnUI()
                .ToDiffedChangeSet(cur => cur.ModId, cur => cur)
                .Bind(out _mods)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.LoadoutId)
                .SelectMany(loadoutRegistry.RevisionsAsLoadouts)
                .Select(loadout => loadout.Name)
                .BindTo(this, vm => vm.LoadoutName);

            _columns.Connect()
                .Bind(out _filteredColumns)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);


        });
    }

    public Task AddMod(string path)
    {
        var file = _fileSystem.FromUnsanitizedFullPath(path);
        if (!_fileSystem.FileExists(file))
        {
            _logger.LogError("File {File} does not exist, not installing mod",
                file);
            return Task.CompletedTask;
        }

        var _ = Task.Run(async () =>
        {
            var analyzedFile = await _archiveAnalyzer.AnalyzeFileAsync(file, CancellationToken.None);
            await _archiveInstaller.AddMods(LoadoutId, analyzedFile.Hash, token: CancellationToken.None);
        });

        return Task.CompletedTask;
    }

    public Task DeleteMods(IEnumerable<ModId> modsToDelete, string commitMessage)
    {
        _loadoutRegistry.Alter(LoadoutId, commitMessage, loadout =>
        {
            var mods = loadout.Mods;
            foreach (var modId in modsToDelete)
            {
                mods = mods.Without(modId);
            }
            return loadout with { Mods = mods };
        });
        return Task.CompletedTask;
    }
}
