using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion;
using NexusMods.App.UI.Pages.ModInfo;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Extensions.DynamicData;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

[UsedImplicitly]
public class LoadoutGridViewModel : APageViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LoadoutGridViewModel> _logger;
    private readonly IConnection _conn;
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly IFileOriginRegistry _fileOriginRegistry;
    private readonly IServiceProvider _provider;

    private ReadOnlyObservableCollection<ModId> _mods;
    public ReadOnlyObservableCollection<ModId> Mods => _mods;

    private readonly SourceCache<IDataGridColumnFactory<LoadoutColumn> ,LoadoutColumn> _columns;

    private ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory<LoadoutColumn>>());
    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns => _filteredColumns;

    [Reactive] public LoadoutId LoadoutId { get; set; }
    [Reactive] public string LoadoutName { get; set; } = "";
    public GameDomain _gameDomain = GameDomain.DefaultValue;

    [Reactive] public ModId[] SelectedItems { get; set; } = Array.Empty<ModId>();
    public ReactiveCommand<NavigationInformation, Unit> ViewModContentsCommand { get; }

    public LoadoutGridViewModel() : base(null!)
    {
        throw new NotImplementedException();
    }
    public LoadoutGridViewModel(
        ILogger<LoadoutGridViewModel> logger,
        IServiceProvider provider,
        IConnection conn,
        IRepository<Loadout.Model> loadoutRepository,
        IFileSystem fileSystem,
        IArchiveInstaller archiveInstaller,
        IFileOriginRegistry fileOriginRegistry,
        IWindowManager windowManager,
        ISettingsManager settingsManager) : base(windowManager)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _conn = conn;
        _archiveInstaller = archiveInstaller;
        _fileOriginRegistry = fileOriginRegistry;
        _provider = provider;

        _columns = new SourceCache<IDataGridColumnFactory<LoadoutColumn>, LoadoutColumn>(_ => throw new NotSupportedException());
        _mods = new ReadOnlyObservableCollection<ModId>(new ObservableCollection<ModId>());
        
        TabIcon = IconValues.Collections;
        TabTitle = Language.LoadoutLeftMenuViewModel_LoadoutGridEntry;

        var nameColumn = provider.GetRequiredService<DataGridColumnFactory<IModNameViewModel, ModId, LoadoutColumn>>();
        nameColumn.Type = LoadoutColumn.Name;
        nameColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

        var categoryColumn = provider.GetRequiredService<DataGridColumnFactory<IModCategoryViewModel, ModId, LoadoutColumn>>();
        categoryColumn.Type = LoadoutColumn.Category;

        var installedColumn = provider.GetRequiredService<DataGridColumnFactory<IModInstalledViewModel, ModId, LoadoutColumn>>();
        installedColumn.Type = LoadoutColumn.Installed;

        var enabledColumn = provider.GetRequiredService<DataGridColumnFactory<IModEnabledViewModel, ModId, LoadoutColumn>>();
        enabledColumn.Type = LoadoutColumn.Enabled;

        var versionColumn = provider.GetRequiredService<DataGridColumnFactory<IModVersionViewModel, ModId, LoadoutColumn>>();
        versionColumn.Type = LoadoutColumn.Version;

        _columns.Edit(x =>
        {
            x.AddOrUpdate(nameColumn, LoadoutColumn.Name);
            x.AddOrUpdate(versionColumn, LoadoutColumn.Version);
            x.AddOrUpdate(categoryColumn, LoadoutColumn.Category);
            x.AddOrUpdate(installedColumn, LoadoutColumn.Installed);
            x.AddOrUpdate(enabledColumn, LoadoutColumn.Enabled);
        });

        var hasSelection = this.WhenAnyValue(vm => vm.SelectedItems, arr => arr.Length != 0);

        ViewModContentsCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var modId = SelectedItems[0];

            var pageData = new PageData
            {
                Context = new ModInfoPageContext
                {
                    LoadoutId = LoadoutId,
                    ModId = modId,
                    Section = CurrentModInfoSection.Files,
                },
                FactoryId = ModInfoPageFactory.StaticId,
            };

            var workspaceController = GetWorkspaceController();
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info, IdBundle);
            workspaceController.OpenPage(WorkspaceId, pageData, behavior);
        }, hasSelection);

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.LoadoutId)
                .SelectMany(id => loadoutRepository.Revisions(id.Value))
                .Select(loadout =>
                {
                    _gameDomain = loadout.Installation.Game.Domain;

                    var settings = settingsManager.Get<LoadoutGridSettings>();
                    var showGameFiles = settings.ShowGameFiles;
                    var showOverride = settings.ShowOverride;
                    
                    return loadout.Mods
                        .Where(m => showGameFiles || m.Category != ModCategory.GameFiles)
                        .Where(m => showOverride || m.Category != ModCategory.Overrides)
                        .Select(m => m.ModId);
                })
                .OnUI()
                .ToDiffedChangeSet(cur => cur, cur => cur)
                .Bind(out _mods)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);

            _columns.Connect()
                .Bind(out _filteredColumns)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);
            
        });
    }

    public Task AddMod(string path) => AddMod(path, installer: null);

    public Task AddModAdvanced(string path)
    {
        var installer = _provider.GetKeyedService<IModInstaller>("AdvancedInstaller");
        return AddMod(path, installer);
    }

    private Task AddMod(string path, IModInstaller? installer)
    {
        var file = _fileSystem.FromUnsanitizedFullPath(path);
        if (!_fileSystem.FileExists(file))
        {
            _logger.LogError("File {File} does not exist, not installing mod", file);
            return Task.CompletedTask;
        }

        var _ = Task.Run(async () =>
        {
            var downloadId = await _fileOriginRegistry.RegisterDownload(file,
                (tx, id) =>
                {
                    tx.Add(id, DownloaderState.GameDomain, _gameDomain);
                    tx.Add(id, FilePathMetadata.OriginalName, file.FileName);
                });
            await _archiveInstaller.AddMods(LoadoutId, downloadId, file.FileName, token: CancellationToken.None, installer: installer);
        });

        return Task.CompletedTask;
    }

    public async Task DeleteMods(IEnumerable<ModId> modsToDelete, string commitMessage)
    {
        var db = _conn.Db;
        var loadout = db.Get(LoadoutId);
        using var tx = _conn.BeginTransaction();
        foreach (var modId in modsToDelete)
        {
            var mod = db.Get(modId);
            foreach (var file in mod.Files)
            {
                tx.Retract(file.Id, File.Loadout, file.LoadoutId.Value);
            }
            tx.Retract(modId.Value, Mod.Loadout, mod.LoadoutId.Value);
        }
        loadout.Revise(tx);
        await tx.Commit();
    }
}
