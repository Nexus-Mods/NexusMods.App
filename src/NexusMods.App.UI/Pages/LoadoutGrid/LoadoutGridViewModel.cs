using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using DynamicData.PLinq;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;
using NexusMods.App.UI.Pages.LoadoutGroupFiles;
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

namespace NexusMods.App.UI.Pages.LoadoutGrid;

[UsedImplicitly]
public class LoadoutGridViewModel : APageViewModel<ILoadoutGridViewModel>, ILoadoutGridViewModel
{
    private readonly IConnection _connection;

    [Reactive] public LoadoutId LoadoutId { get; set; }
    private ReadOnlyObservableCollection<LoadoutItemGroupId> _groupIds = ReadOnlyObservableCollection<LoadoutItemGroupId>.Empty;
    public ReadOnlyObservableCollection<LoadoutItemGroupId> GroupIds => _groupIds;

    private readonly SourceCache<IDataGridColumnFactory<LoadoutColumn> ,LoadoutColumn> _columns = new(_ => throw new NotSupportedException());
    private readonly ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> _filteredColumns;
    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns => _filteredColumns;

    public SourceList<LoadoutItemGroupId> SelectedGroupIds { get; } = new();

    public ReactiveCommand<NavigationInformation, Unit> ViewLibraryCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> ViewFilesCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> DeleteCommand { get; }

    [Reactive] public string? EmptyStateTitle { get; [UsedImplicitly] private set; }

    public LoadoutGridViewModel(
        ILogger<LoadoutGridViewModel> logger,
        IServiceProvider provider,
        IConnection conn,
        IWindowManager windowManager,
        ISettingsManager settingsManager) : base(windowManager)
    {
        _connection = conn;

        TabIcon = IconValues.Collections;
        TabTitle = Language.LoadoutLeftMenuViewModel_LoadoutGridEntry;

        var nameColumn = provider.GetRequiredService<DataGridColumnFactory<IModNameViewModel, LoadoutItemGroupId, LoadoutColumn>>();
        nameColumn.Type = LoadoutColumn.Name;
        nameColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

        var installedColumn = provider.GetRequiredService<DataGridColumnFactory<IModInstalledViewModel, LoadoutItemGroupId, LoadoutColumn>>();
        installedColumn.Type = LoadoutColumn.Installed;

        var enabledColumn = provider.GetRequiredService<DataGridColumnFactory<IModEnabledViewModel, LoadoutItemGroupId, LoadoutColumn>>();
        enabledColumn.Type = LoadoutColumn.Enabled;

        _columns.Edit(x =>
        {
            x.AddOrUpdate(nameColumn, LoadoutColumn.Name);
            x.AddOrUpdate(installedColumn, LoadoutColumn.Installed);
            x.AddOrUpdate(enabledColumn, LoadoutColumn.Enabled);
        });

        _columns
            .Connect()
            .Bind(out _filteredColumns)
            .SubscribeWithErrorLogging(logger);

        ViewLibraryCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var pageData = new PageData
            {
                Context = new FileOriginsPageContext
                {
                    LoadoutId = LoadoutId,
                },
                FactoryId = FileOriginsPageFactory.StaticId,
            };

            var workspaceController = GetWorkspaceController();
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            workspaceController.OpenPage(WorkspaceId, pageData, behavior);
        });
        
        var settings = settingsManager.GetChanges<LoadoutGridSettings>()
            .StartWith(settingsManager.Get<LoadoutGridSettings>());

        var hasSelection = SelectedGroupIds.CountChanged.Select(count => count > 0);

        ViewFilesCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var groupId = SelectedGroupIds.Items.First();

            var pageData = new PageData
            {
                FactoryId = LoadoutGroupFilesPageFactory.StaticId,
                Context = new LoadoutGroupFilesPageContext
                {
                    GroupId = groupId,
                },
            };

            var workspaceController = GetWorkspaceController();
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            workspaceController.OpenPage(WorkspaceId, pageData, behavior);
        }, hasSelection);

        DeleteCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            // TODO:
        }, hasSelection);

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.LoadoutId)
                .Select(loadoutId => Loadout.Observe(_connection, loadoutId))
                .Switch()
                .Select(loadout => loadout
                    .Items
                    .OfTypeLoadoutItemGroup()
                    .Where(group => !group.Contains(LoadoutItem.ParentId))
                    .Select(group => group.LoadoutItemGroupId)
                )
                .OnUI()
                .ToDiffedChangeSet(group => group, group => group)
                .Filter(settings.Select(ShouldShow))
                .Bind(out _groupIds)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.LoadoutId)
                .Select(loadoutId => Loadout.Load(_connection.Db, loadoutId))
                .Select(loadout => string.Format(Language.LoadoutGridViewModel_EmptyModlistTitleString, loadout.InstallationInstance.Game.Name))
                .BindToVM(this, vm => vm.EmptyStateTitle)
                .DisposeWith(d);
        });
    }

    private Func<LoadoutItemGroupId, bool> ShouldShow(LoadoutGridSettings settings)
    {
        return itm =>
        {
            var group = LoadoutItemGroup.Load(_connection.Db, itm);
        
            if (group.Contains(LoadoutGameFilesGroup.GameMetadata) && !settings.ShowGameFiles)
                return false;
        
            if (group.Contains(LoadoutOverridesGroup.OverridesForId) && !settings.ShowOverride)
                return false;

            return true;
        };
    }

    // [UsedImplicitly]
    // public async Task DeleteMods(IEnumerable<ModId> modsToDelete, string commitMessage)
    // {
    //     var db = _conn.Db;
    //     var loadout = Loadout.Load(db, LoadoutId);
    //     using var tx = _conn.BeginTransaction();
    //     foreach (var modId in modsToDelete)
    //     {
    //         var mod = Mod.Load(db, modId);
    //         foreach (var file in mod.Files)
    //         {
    //             tx.Retract(file.Id, File.Loadout, file.LoadoutId.Value);
    //         }
    //         tx.Retract(modId.Value, Mod.Loadout, mod.LoadoutId.Value);
    //     }
    //     loadout.Revise(tx);
    //     await tx.Commit();
    // }
}
