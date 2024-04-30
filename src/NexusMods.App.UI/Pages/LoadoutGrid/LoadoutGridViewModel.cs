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
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion;
using NexusMods.App.UI.Pages.ModInfo;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Extensions.DynamicData;
using NexusMods.Icons;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

[UsedImplicitly]
public class LoadoutGridViewModel : APageViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LoadoutGridViewModel> _logger;
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly IFileOriginRegistry _fileOriginRegistry;
    private readonly IServiceProvider _provider;

    private ReadOnlyObservableCollection<ModCursor> _mods;
    public ReadOnlyObservableCollection<ModCursor> Mods => _mods;

    private readonly SourceCache<IDataGridColumnFactory<LoadoutColumn> ,LoadoutColumn> _columns;

    private ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory<LoadoutColumn>>());
    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns => _filteredColumns;

    [Reactive] public LoadoutId LoadoutId { get; set; }
    [Reactive] public string LoadoutName { get; set; } = "";

    [Reactive] public ModCursor[] SelectedItems { get; set; } = Array.Empty<ModCursor>();
    public ReactiveCommand<NavigationInformation, Unit> ViewModContentsCommand { get; }

    public LoadoutGridViewModel(
        ILogger<LoadoutGridViewModel> logger,
        IServiceProvider provider,
        ILoadoutRegistry loadoutRegistry,
        IFileSystem fileSystem,
        IArchiveInstaller archiveInstaller,
        IFileOriginRegistry fileOriginRegistry,
        IWindowManager windowManager) : base(windowManager)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _loadoutRegistry = loadoutRegistry;
        _archiveInstaller = archiveInstaller;
        _fileOriginRegistry = fileOriginRegistry;
        _provider = provider;

        _columns = new SourceCache<IDataGridColumnFactory<LoadoutColumn>, LoadoutColumn>(_ => throw new NotSupportedException());
        _mods = new ReadOnlyObservableCollection<ModCursor>(new ObservableCollection<ModCursor>());
        
        TabIcon = IconValues.Collections;

        var nameColumn = provider.GetRequiredService<DataGridColumnFactory<IModNameViewModel, ModCursor, LoadoutColumn>>();
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

        var hasSelection = this.WhenAnyValue(vm => vm.SelectedItems, arr => arr.Length != 0);

        ViewModContentsCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var modId = SelectedItems[0].ModId;

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
                .SelectMany(loadoutRegistry.RevisionsAsLoadouts)
                .Select(loadout => loadout.Mods.Values
                    // NOTE(erri120): see https://github.com/Nexus-Mods/NexusMods.App/issues/1195 for details
                    .Where(mod => mod.ModCategory != Mod.GameFilesCategory)
                    .Select(mod => new ModCursor(loadout.LoadoutId, mod.Id))
                )
                .OnUI()
                .ToDiffedChangeSet(cur => cur.ModId, cur => cur)
                .Bind(out _mods)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.LoadoutId)
                .SelectMany(loadoutRegistry.RevisionsAsLoadouts)
                .Select(loadout => loadout.Name)
                .OnUI()
                .Do(loadoutName =>
                {
                    TabTitle = loadoutName;
                })
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
            _logger.LogError("File {File} does not exist, not installing mod", file);
            return Task.CompletedTask;
        }

        var _ = Task.Run(async () =>
        {
            var downloadId = await _fileOriginRegistry.RegisterDownload(file,
                (tx, id) =>
                {
                    tx.Add(id, FilePathMetadata.OriginalName, file.FileName);
                });
            await _archiveInstaller.AddMods(LoadoutId, downloadId, token: CancellationToken.None);
        });

        return Task.CompletedTask;
    }

    public Task AddModAdvanced(string path)
    {
        var file = _fileSystem.FromUnsanitizedFullPath(path);
        if (!_fileSystem.FileExists(file))
        {
            _logger.LogError("File {File} does not exist, not installing mod", file);
            return Task.CompletedTask;
        }

        // this is one of the worst hacks I've done in recent years, it's bad, and I should feel bad
        // Likely could be solved by .NET 8's DI improvements, with Keyed services
        var installer = _provider
            .GetRequiredService<IEnumerable<IModInstaller>>()
            .First(i => i.GetType().Name.Contains("AdvancedManualInstaller"));

        var _ = Task.Run(async () =>
        {
            var downloadId = await _fileOriginRegistry.RegisterDownload(file,
                (tx, id) =>
                {
                    tx.Add(id, FilePathMetadata.OriginalName, file.FileName);
                });
            await _archiveInstaller.AddMods(LoadoutId, downloadId, token: CancellationToken.None, installer: installer);
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

    public void ViewModContents(ModId[] toView)
    {

    }
}
