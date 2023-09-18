using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModInstalled;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModName;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModVersion;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveMetaData;
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


    private readonly SourceCache<IDataGridColumnFactory<LoadoutColumn> ,LoadoutColumn> _columns;
    private ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory<LoadoutColumn>>());
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LoadoutGridViewModel> _logger;
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly IDownloadRegistry _downloadRegistry;

    [Reactive]
    public string LoadoutName { get; set; } = "";
    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns => _filteredColumns;


    public LoadoutGridViewModel(
        ILogger<LoadoutGridViewModel> logger,
        IServiceProvider provider,
        LoadoutRegistry loadoutRegistry,
        IFileSystem fileSystem,
        IArchiveInstaller archiveInstaller,
        IDownloadRegistry downloadRegistry)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _loadoutRegistry = loadoutRegistry;
        _archiveInstaller = archiveInstaller;
        _downloadRegistry = downloadRegistry;

        _columns =
            new SourceCache<IDataGridColumnFactory<LoadoutColumn>, LoadoutColumn>(
                x => throw new NotImplementedException());

        _mods = new ReadOnlyObservableCollection<ModCursor>(
            new ObservableCollection<ModCursor>());

        var nameColumn = provider
            .GetRequiredService<
                DataGridColumnFactory<IModNameViewModel, ModCursor, LoadoutColumn>>();
        nameColumn.Type = LoadoutColumn.Name;
        nameColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        var categoryColumn = provider.GetRequiredService<DataGridColumnFactory<IModCategoryViewModel, ModCursor, LoadoutColumn>>();
        categoryColumn.Type = LoadoutColumn.Category;
        var installedColumn = provider.GetRequiredService<DataGridColumnFactory<IModInstalledViewModel, ModCursor, LoadoutColumn>>();
        installedColumn.Type = LoadoutColumn.Installed;
        var enabledColumn = provider.GetRequiredService<DataGridColumnFactory<IModEnabledViewModel, ModCursor, LoadoutColumn>>();
        enabledColumn.Type = LoadoutColumn.Enabled;
        var versionColumn = provider.GetRequiredService<DataGridColumnFactory<IModVersionViewModel, ModCursor, LoadoutColumn>>();
        versionColumn.Type = LoadoutColumn.Version;

        _columns.Edit(x =>
        {
            x.AddOrUpdate(nameColumn, LoadoutColumn.Name);
            x.AddOrUpdate(versionColumn, LoadoutColumn.Version);
            x.AddOrUpdate(categoryColumn, LoadoutColumn.Category);
            x.AddOrUpdate(installedColumn, LoadoutColumn.Installed);
            x.AddOrUpdate(enabledColumn, LoadoutColumn.Enabled);
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
            var downloadId = await _downloadRegistry.RegisterDownload(file,
                new FilePathMetadata
                {
                    OriginalName = file.FileName,
                    Quality = Quality.Low,
                    Name = file.FileName
                });
            await _archiveInstaller.AddMods(LoadoutId, downloadId, token: CancellationToken.None);
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
