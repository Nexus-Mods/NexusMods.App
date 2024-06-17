using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
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
using NexusMods.App.UI.Pages.ModLibrary;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Extensions.DynamicData;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

[UsedImplicitly]
public class LoadoutGridViewModel : APageViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    private readonly IConnection _conn;

    private ReadOnlyObservableCollection<ModId> _mods;
    public ReadOnlyObservableCollection<ModId> Mods => _mods;

    private readonly SourceCache<IDataGridColumnFactory<LoadoutColumn> ,LoadoutColumn> _columns;

    private ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> _filteredColumns = new([]);
    
    
    [Reactive] public string? EmptyModlistTitleMessage { get; private set; }
    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns => _filteredColumns;

    [Reactive] public LoadoutId LoadoutId { get; set; }
    [Reactive] public string LoadoutName { get; set; } = "";

    [Reactive] public ModId[] SelectedItems { get; set; } = [];
    public ReactiveCommand<NavigationInformation, Unit> ViewModContentsCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> ViewModLibraryCommand { get; }

    public LoadoutGridViewModel() : base(null!)
    {
        throw new NotImplementedException();
    }
    public LoadoutGridViewModel(
        ILogger<LoadoutGridViewModel> logger,
        IServiceProvider provider,
        IConnection conn,
        IRepository<Loadout.Model> loadoutRepository,
        IWindowManager windowManager,
        ISettingsManager settingsManager) : base(windowManager)
    {
        _conn = conn;

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
        
        ViewModLibraryCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {

            var pageData = new PageData
            {
                Context = new FileOriginsPageContext()
                {
                    LoadoutId = LoadoutId,
                },
                FactoryId = FileOriginsPageFactory.StaticId,
            };

            var workspaceController = GetWorkspaceController();
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info, IdBundle);
            workspaceController.OpenPage(WorkspaceId, pageData, behavior);
        });

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.LoadoutId)
                .CombineLatest(settingsManager.GetChanges<LoadoutGridSettings>(prependCurrent: true))
                .SelectMany(tuple => loadoutRepository.Revisions(tuple.First.Value))
                .Select(loadout =>
                {
                    
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
            
            this.WhenAnyValue(vm => vm.LoadoutId)
                .Select(id => conn.Db.Get(id))
                .WhereNotNull()
                .SubscribeWithErrorLogging(loadout =>
                {
                    EmptyModlistTitleMessage = GetEmptyModlistTitleString(loadout.Installation);
                })
                .DisposeWith(d);
            
        });
    }

    [UsedImplicitly]
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
    
    private const string NexusModsUrl = "https://www.nexusmods.com/{0}";
    private static string GetEmptyModlistTitleString(GameInstallation gameInstallation)
    {
        return string.Format(Language.LoadoutGridViewModel_EmptyModlistTitleString, gameInstallation.Game.Name);
    }

}
