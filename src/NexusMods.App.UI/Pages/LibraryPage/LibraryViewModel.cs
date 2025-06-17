using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Library;
using NexusMods.App.UI.Pages.LibraryPage.Collections;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Cascade;
using NexusMods.Collections;
using NexusMods.CrossPlatform.Process;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.UpdateFilters;
using NexusMods.Paths;
using ObservableCollections;
using OneOf;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IConnection _connection;
    private readonly ILibraryService _libraryService;

    public string EmptyLibrarySubtitleText { get; }

    public ReactiveCommand<Unit> UpdateAllCommand { get; }
    public ReactiveCommand<Unit> RefreshUpdatesCommand { get; }
    public ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }

    public ReactiveCommand<Unit> RemoveSelectedItemsCommand { get; }
    
    public ReactiveCommand<Unit> DeselectItemsCommand { get; }

    public ReactiveCommand<Unit> OpenFilePickerCommand { get; }

    public ReactiveCommand<Unit> OpenNexusModsCommand { get; }
    public ReactiveCommand<Unit> OpenNexusModsCollectionsCommand { get; }

    [Reactive] public int SelectionCount { get; private set; }
    
    [Reactive] public IStorageProvider? StorageProvider { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILibraryItemInstaller _advancedInstaller;
    private readonly IGameDomainToGameIdMappingCache _gameIdMappingCache;
    private readonly Loadout.ReadOnly _loadout;
    private readonly IModUpdateService _modUpdateService;
    private readonly ILoginManager _loginManager;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly TemporaryFileManager _temporaryFileManager;

    public LibraryTreeDataGridAdapter Adapter { get; }
    private ReadOnlyObservableCollection<ICollectionCardViewModel> _collections = new([]);
    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections => _collections;

    private ReadOnlyObservableCollection<InstallationTarget> _installationTargets = new([]);
    public ReadOnlyObservableCollection<InstallationTarget> InstallationTargets => _installationTargets;

    [Reactive] public InstallationTarget? SelectedInstallationTarget { get; set; }

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        IGameDomainToGameIdMappingCache gameIdMappingCache,
        LoadoutId loadoutId) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _gameIdMappingCache = gameIdMappingCache;
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _modUpdateService = serviceProvider.GetRequiredService<IModUpdateService>();
        _loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();

        var collectionDownloader = new CollectionDownloader(serviceProvider);
        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

        var loadout = Loadout.Load(_connection.Db, loadoutId);
        var libraryFilter = new LibraryFilter(loadout, loadout.InstallationInstance.Game);

        Adapter = new LibraryTreeDataGridAdapter(serviceProvider, libraryFilter);

        _advancedInstaller = serviceProvider.GetRequiredKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller_Direct");

        TabTitle = Language.LibraryPageTitle;
        TabIcon = IconValues.LibraryOutline;
        
        _loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = _loadout.InstallationInstance.Game;

        EmptyLibrarySubtitleText = string.Format(Language.FileOriginsPageViewModel_EmptyLibrarySubtitleText, game.Name);

        DeselectItemsCommand = new ReactiveCommand<Unit>(_ =>
        {
            Adapter.ClearSelection();
        });

        RefreshUpdatesCommand = new ReactiveCommand<Unit>(
            executeAsync: (_, token) => RefreshUpdates(token),
            awaitOperation: AwaitOperation.Switch
        );
        
        UpdateAllCommand = new ReactiveCommand<Unit>(_ => throw new NotImplementedException("[Update All] This feature is not yet implemented, please wait for the next release."));

        var hasSelection = Adapter.SelectedModels
            .ObserveCountChanged()
            .Select(count => count > 0);
        
        InstallSelectedItemsCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => InstallSelectedItems(useAdvancedInstaller: false, cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        InstallSelectedItemsWithAdvancedInstallerCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => InstallSelectedItems(useAdvancedInstaller: true, cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        RemoveSelectedItemsCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => RemoveSelectedItems(cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        var canUseFilePicker = this.WhenAnyValue(vm => vm.StorageProvider)
            .ToObservable()
            .WhereNotNull()
            .Select(x => x.CanOpen);

        OpenFilePickerCommand = canUseFilePicker.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => AddFilesFromDisk(StorageProvider!, cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: true,
            configureAwait: false
        );

        var osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        OpenNexusModsCommand = new ReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) =>
            {
                var gameDomain = _gameIdMappingCache[game.GameId];
                var gameUri = NexusModsUrlBuilder.GetGameUri(gameDomain);
                await osInterop.OpenUrl(gameUri, cancellationToken: cancellationToken);
            },
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );
        OpenNexusModsCollectionsCommand = new ReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) =>
            {
                var gameDomain = _gameIdMappingCache[game.GameId];
                var gameUri = NexusModsUrlBuilder.GetBrowseCollectionsUri(gameDomain);
                await osInterop.OpenUrl(gameUri, cancellationToken: cancellationToken);
            },
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, static vm => vm.StorageProvider = null).AddTo(disposables);
            Adapter.Activate().AddTo(disposables);

            Adapter.MessageSubject.SubscribeAwait(
                onNextAsync: async (message, cancellationToken) =>
                {
                    await message.Match(
                        async installMessage =>
                        {
                            foreach (var id in installMessage.Ids)
                            {
                                var libraryItem = LibraryItem.Load(_connection.Db, id);
                                if (!libraryItem.IsValid()) continue;
                                await InstallLibraryItem(libraryItem, _loadout, GetInstallationTarget(), cancellationToken);
                            }
                        },
                        async updateAndReplaceMessage => await HandleUpdateAndReplaceMessage(updateAndReplaceMessage, cancellationToken),
                        async updateAndKeepOldMessage => await HandleUpdateAndKeepOldMessage(updateAndKeepOldMessage, cancellationToken),
                        async viewChangelogMessage => await HandleViewChangelogMessage(viewChangelogMessage, cancellationToken),
                        async viewModPageMessage => await HandleViewModPageMessage(viewModPageMessage, cancellationToken),
                        async hideUpdatesMessage => await HandleHideUpdatesMessage(hideUpdatesMessage, cancellationToken)
                    );
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);

            _connection.Topology
                .Observe(Loadout.MutableCollections.Where(tuple => tuple.Loadout == loadoutId.Value))
                .Transform(tuple =>
                {
                    var group = CollectionGroup.Load(_connection.Db, tuple.CollectionGroup);
                    return new InstallationTarget(group.CollectionGroupId, group.AsLoadoutItemGroup().AsLoadoutItem().Name);
                })
                .AddKey(x => x.Id)
                .SortAndBind(out _installationTargets, Comparer<InstallationTarget>.Create((a,b) => a.Id.Value.CompareTo(b.Id.Value)))
                .Subscribe()
                .AddTo(disposables);

            CollectionRevisionMetadata.ObserveAll(_connection)
                .FilterImmutable(revision => revision.Collection.GameId == game.GameId)
                .OnUI()
                .Transform(ICollectionCardViewModel (revision) => new CollectionCardViewModel(
                    collectionDownloader: collectionDownloader,
                    tileImagePipeline: tileImagePipeline,
                    userAvatarPipeline: userAvatarPipeline,
                    windowManager: WindowManager,
                    workspaceId: WorkspaceId,
                    revision: revision,
                    targetLoadout: _loadout)
                )
                .Bind(out _collections)
                .Subscribe()
                .AddTo(disposables);
            
            // Update the selection count based on the selected models
            Adapter.SelectedModels
                .ObserveChanged()
                .Select(_ => GetSelectedIds().Length)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(count => SelectionCount = count);

            // Auto check updates on entering library.
            RefreshUpdatesCommand.Execute(Unit.Default);
        });
    }

    private async ValueTask HandleUpdateAndReplaceMessage(UpdateAndReplaceMessage updateAndReplaceMessage, CancellationToken cancellationToken)
    {
        // TODO: Implement this
    }
    
    private async ValueTask HandleUpdateAndKeepOldMessage(UpdateAndKeepOldMessage updateAndKeepOldMessage, CancellationToken cancellationToken)
    {
        var updatesOnPage = updateAndKeepOldMessage.Updates;
        
        // Note(sewer)
        // If the user is a free user, they have to go to the website due to API restrictions.
        // For premium, we can start a download directly.
        var isPremium = _loginManager.IsPremium;
        if (!isPremium)
        {
            /*
               // Note(sewer): The commented code here is the correct behaviour
               // as intended per the phase one design. We temporarily need to alter
               // this behaviour due to the TreeDataGrid bug. When TreeDataGrid
               // is fixed, we can revert.

               // If there are multiple mods, we expand the row
               var treeNode = updateMessage.TreeNode;
               if (updatesOnPage.NumberOfModFilesToUpdate > 1)
               {
                   treeNode.IsExpanded = true; // 👈 TreeDataGrid bug. Doesn't handle PropertyChanged right.
               }
               else
               {
                   // Otherwise send them to the download page!!
                   var latestFile = updatesOnPage.NewestFile();
                   var modFileUrl = NexusModsUrlBuilder.CreateModFileDownloadUri(latestFile.Uid.FileId, latestFile.Uid.GameId);
                   var osInterop = _serviceProvider.GetRequiredService<IOSInterop>();
                   await osInterop.OpenUrl(modFileUrl, cancellationToken: cancellationToken);
               }
            */

            // Open download page for every unique file.
            foreach (var file in updatesOnPage.NewestUniqueFileForEachMod())
            {
                var osInterop = _serviceProvider.GetRequiredService<IOSInterop>();
                var uri = NexusModsUrlBuilder.GetFileDownloadUri(file.ModPage.GameDomain, file.ModPage.Uid.ModId, file.Uid.FileId, useNxmLink: true, campaign: NexusModsUrlBuilder.CampaignUpdates);
                await osInterop.OpenUrl(uri, cancellationToken: cancellationToken);
            }
        }
        else
        {
            // Note(sewer): There's usually just 1 file in like 99% of the cases here
            //              so no need to optimize around file reuse and TemporaryFileManager.
            foreach (var newestFile in updatesOnPage.NewestUniqueFileForEachMod())
            {
                await using var tempPath = _temporaryFileManager.CreateFile();
                var job = await _nexusModsLibrary.CreateDownloadJob(tempPath, newestFile, cancellationToken: cancellationToken);
                await _libraryService.AddDownload(job);
            }
        }
    }
    
    private ValueTask HandleViewChangelogMessage(ViewChangelogMessage viewChangelogMessage, CancellationToken cancellationToken)
    {
/*
Note(sewer): This method currently sends you to the mod page due to technical limitations.
A summarised quote/explanation from myself below:

- On the site, there is no true 'per file' level changelog, but only a changelog at the mod page level currently.
    - The widget which shows the changes e.g. https://www.nexusmods.com/Core/Libs/Common/Widgets/ModChangeLogPopUp?mod_id=2347&game_id=1704&version=10.0.1 
      cannot be explicitly opened via hyperlink (due to a recent change on the site to block this).
    - From observation, I think those are automatically added when the user sets the version of the file to be the 
      same as the version of a changelog item made on the mod page.
- For rendering it myself: GraphQL API has a `changelogText` field for mod files.
    - When present, the values are listed in the form `#<ModChangelog:0x00007f060e21e880>` .
    - But there's no ModChangelog type or field anywhere in the autogenerated API docs.
    - I imagine you can manually hack this together by using V1 API, but code would be quickly obsolete.
- Some mod authors put the changelog in the file description; rather than on page level changelog.

After asking design, we're choosing to simply open the mod page for now.
*/
        return viewChangelogMessage.Id.Match(
            modPageMetadataId => OpenModPage(modPageMetadataId, cancellationToken),
            libraryItemId => OpenModPage(new NexusModsLibraryItem.ReadOnly(_connection.Db, libraryItemId).ModPageMetadataId, cancellationToken)
        );
    }
    
    private ValueTask HandleViewModPageMessage(ViewModPageMessage viewModPageMessage, CancellationToken cancellationToken)
    {
        return viewModPageMessage.Id.Match(
            modPageMetadataId => OpenModPage(modPageMetadataId, cancellationToken),
            libraryItemId => OpenModPage(new NexusModsLibraryItem.ReadOnly(_connection.Db, libraryItemId).ModPageMetadataId, cancellationToken)
        );
    }
    
    private async ValueTask HandleHideUpdatesMessage(HideUpdatesMessage hideUpdatesMessage, CancellationToken cancellationToken)
    {
        var modUpdateFilterService = _serviceProvider.GetRequiredService<IModUpdateFilterService>();

        await hideUpdatesMessage.Id.Match(
            async modPageId =>
            {
                // Handle hiding/showing updates for a mod page
                // First get all library items we have that come from the mod page.
                var allLibraryFilesForThisMod =  NexusModsLibraryItem.All(_connection.Db)
                    .Where(x => x.ModPageMetadataId == modPageId)
                    .Select(x => x.FileMetadata)
                    .ToArray();

                // Note(sewer):
                // Behaviour per captainsandypants (Slack).
                // 'If any children have updates set to hidden, then the parent should have "Show updates" as the menu item.
                // When selected, this will set all children to show updates.'
                //
                // We have to be careful here.
                // We have a list of ***current versions*** of files in the mod page.
                // We need to check if it's possible to update ***any of these current versions*** to a new file.
                //     👉 👉 So for each file we need to check if any of its versions is not ignored as an update.
                var modsWithUpdatesHidden = allLibraryFilesForThisMod.Where(x =>
                    {
                        // Checking all versions is not a bug, it is intended behaviour per design.
                        // Search 'That definition also means older versions should be excluded from update checks.' in Slack.
                        var newerFiles = RunUpdateCheck.GetAllVersionsForExistingFile(x).ToArray();
                        return newerFiles.All(newer => modUpdateFilterService.IsFileHidden(newer.Uid));
                    }
                ).ToArray();

                if (modsWithUpdatesHidden.Length > 0)
                {
                    var allVersionsOfLibraryItemsWithUpdatesHidden =
                        modsWithUpdatesHidden.SelectMany(RunUpdateCheck.GetAllVersionsForExistingFile)
                                             .Select(x => x.Uid);
                    await modUpdateFilterService.ShowFilesAsync(allVersionsOfLibraryItemsWithUpdatesHidden);
                }
                else
                {
                    var allVersionsOfAllFilesForThisMod = allLibraryFilesForThisMod
                        .SelectMany(RunUpdateCheck.GetAllVersionsForExistingFile)
                        .Select(x => x.Uid);
                    await modUpdateFilterService.HideFilesAsync(allVersionsOfAllFilesForThisMod);
                }
            },
            async libraryItemId =>
            {
                // Handle hiding/showing updates for a single file
                var libraryItem = NexusModsLibraryItem.Load(_connection.Db, libraryItemId);
                var fileMetadata = libraryItem.FileMetadata;
                var allVersions = RunUpdateCheck.GetAllVersionsForExistingFile(fileMetadata).ToArray();

                var areAllHidden = allVersions.All(x => modUpdateFilterService.IsFileHidden(x.Uid));

                if (areAllHidden)
                    await modUpdateFilterService.ShowFilesAsync(allVersions.Select(x => x.Uid));
                else
                    await modUpdateFilterService.HideFilesAsync(allVersions.Select(x => x.Uid));
            }
        );
    }

    private ValueTask OpenModPage(NexusModsModPageMetadataId modPageMetadataId, CancellationToken cancellationToken)
    {
        var modPage = new NexusModsModPageMetadata.ReadOnly(_connection.Db, modPageMetadataId);
        var url = NexusModsUrlBuilder.GetModUri(modPage.GameDomain, modPage.Uid.ModId);
        var os = _serviceProvider.GetRequiredService<IOSInterop>();
        // Note(sewer): Don't await, we don't want to block the UI thread when user pops a webpage.
        _ = os.OpenUrl(url, cancellationToken: cancellationToken);
        return ValueTask.CompletedTask;
    }

    // Note(sewer): ValueTask because of R3 constraints with ReactiveCommand API
    private async ValueTask RefreshUpdates(CancellationToken token) 
    {
        await _modUpdateService.CheckAndUpdateModPages(token, notify: true);
    }

    private async ValueTask InstallItems(LibraryItemId[] ids, LoadoutItemGroupId targetLoadoutGroup, bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var items = ids
            .Select(id => LibraryItem.Load(db, id))
            .Where(x => x.IsValid())
            .ToArray();

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: items.Length,
            body: (i, innerCancellationToken) => InstallLibraryItem(items[i], _loadout, targetLoadoutGroup, innerCancellationToken, useAdvancedInstaller),
            cancellationToken: cancellationToken
        );
    }

    private LibraryItemId[] GetSelectedIds()
    {
        var ids = Adapter.SelectedModels
            .Select(static model => model.GetOptional<LibraryComponents.InstallAction>(LibraryColumns.Actions.InstallComponentKey))
            .Where(static optional => optional.HasValue)
            .SelectMany(static optional => optional.Value.ItemIds)
            .Distinct()
            .ToArray();

        return ids;
    }

    private LoadoutItemGroupId GetInstallationTarget() => (SelectedInstallationTarget?.Id ?? _installationTargets[0].Id).Value;

    private ValueTask InstallSelectedItems(bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        return InstallItems(GetSelectedIds(), GetInstallationTarget(), useAdvancedInstaller, cancellationToken);
    }

    private async ValueTask InstallLibraryItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId loadout,
        LoadoutItemGroupId targetLoadoutGroup,
        CancellationToken cancellationToken,
        bool useAdvancedInstaller = false)
    {
        await _libraryService.InstallItem(libraryItem, loadout, parent: targetLoadoutGroup, installer: useAdvancedInstaller ? _advancedInstaller : null);
    }

    private async ValueTask RemoveSelectedItems(CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var toRemove = GetSelectedIds().Select(id => LibraryItem.Load(db, id)).ToArray();
        await LibraryItemRemover.RemoveAsync(_connection, _serviceProvider.GetRequiredService<IOverlayController>(), _libraryService, toRemove);
    }

    private async ValueTask AddFilesFromDisk(IStorageProvider storageProvider, CancellationToken cancellationToken)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = Language.LoadoutGridView_AddMod_FilePicker_Title,
            FileTypeFilter =
            [
                // TODO: fetch from some service
                new FilePickerFileType(Language.LoadoutGridView_AddMod_FileType_Archive)
                {
                    Patterns = ["*.zip", "*.7z", "*.rar"],
                },
            ],
        });

        var paths = files
            .Select(file => file.TryGetLocalPath())
            .NotNull()
            .Select(path => FileSystem.Shared.FromUnsanitizedFullPath(path))
            .Where(path => path.FileExists)
            .ToArray();

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: paths.Length,
            body: async (i, innerCancellationToken) =>
            {
                var path = paths[i];
                await _libraryService.AddLocalFile(path);
            },
            cancellationToken: cancellationToken
        );
    }
}

public readonly record struct InstallMessage(LibraryItemId[] Ids);
public readonly record struct UpdateAndReplaceMessage(ModUpdatesOnModPage Updates, CompositeItemModel<EntityId> TreeNode);
public readonly record struct UpdateAndKeepOldMessage(ModUpdatesOnModPage Updates, CompositeItemModel<EntityId> TreeNode);
public readonly record struct ViewChangelogMessage(OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> Id);
public readonly record struct ViewModPageMessage(OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> Id);
public readonly record struct HideUpdatesMessage(OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> Id);

public class LibraryTreeDataGridAdapter :
    TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>,
    ITreeDataGirdMessageAdapter<OneOf<InstallMessage, UpdateAndReplaceMessage, UpdateAndKeepOldMessage, ViewChangelogMessage, ViewModPageMessage, HideUpdatesMessage>>
{
    private readonly ILibraryDataProvider[] _libraryDataProviders;
    private readonly LibraryFilter _libraryFilter;
    private readonly IConnection _connection;

    public Subject<OneOf<InstallMessage, UpdateAndReplaceMessage, UpdateAndKeepOldMessage, ViewChangelogMessage, ViewModPageMessage, HideUpdatesMessage>> MessageSubject { get; } = new();

    public LibraryTreeDataGridAdapter(IServiceProvider serviceProvider, LibraryFilter libraryFilter)
    {
        _libraryFilter = libraryFilter;
        _libraryDataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        return _libraryDataProviders.Select(x => x.ObserveLibraryItems(_libraryFilter)).MergeChangeSets();
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        model.SubscribeToComponentAndTrack<LibraryComponents.InstallAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.InstallComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandInstall.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, _, component) = state;
                var ids = component.ItemIds.ToArray();

                self.MessageSubject.OnNext(new InstallMessage(ids));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.UpdateAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.UpdateComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandUpdateAndKeepOld
                .SubscribeOnUIThreadDispatcher() // Update payload may expand row for free users, requiring UI thread.
                .Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, component) = state;
                var newFile = component.NewFiles.Value;
                self.MessageSubject.OnNext(new UpdateAndKeepOldMessage(newFile, model));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.UpdateAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.UpdateComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandUpdateAndReplace
                .SubscribeOnUIThreadDispatcher() // Update payload may expand row for free users, requiring UI thread.
                .Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, component) = state;
                var newFile = component.NewFiles.Value;
                self.MessageSubject.OnNext(new UpdateAndReplaceMessage(newFile, model));
            })
        );

        static OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> GetModPageIdOneOfType(IDb db, EntityId entityId)
        {
            var modPageMetadata = NexusModsModPageMetadata.Load(db, entityId);
            if (modPageMetadata.IsValid())
                return OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId>.FromT0(modPageMetadata.Id);
                
            // Try to load as NexusModsLibraryItem
            var libraryItem = NexusModsLibraryItem.Load(db, entityId);
            if (libraryItem.IsValid())
                return OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId>.FromT1(libraryItem.Id);
            
            throw new Exception("Unknown type of entity for ViewChangelogAction: " + entityId);
        }

        model.SubscribeToComponentAndTrack<LibraryComponents.ViewChangelogAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.ViewChangelogComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandViewChangelog.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var entityId = model.Key;
                
                self.MessageSubject.OnNext(new ViewChangelogMessage(GetModPageIdOneOfType(self._connection.Db, entityId)));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.ViewModPageAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.ViewModPageComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandViewModPage.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var entityId = model.Key;

                self.MessageSubject.OnNext(new ViewModPageMessage(GetModPageIdOneOfType(self._connection.Db, entityId)));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.HideUpdatesAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.HideUpdatesComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandHideUpdates.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var entityId = model.Key;

                self.MessageSubject.OnNext(new HideUpdatesMessage(GetModPageIdOneOfType(self._connection.Db, entityId)));
            })
        );
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, SharedColumns.Name>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, LibraryColumns.ItemVersion>(),
            ColumnCreator.Create<EntityId, SharedColumns.ItemSize>(),
            ColumnCreator.Create<EntityId, LibraryColumns.DownloadedDate>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<EntityId, SharedColumns.InstalledDate>(),
            ColumnCreator.Create<EntityId, LibraryColumns.Actions>(),
        ];
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            MessageSubject.Dispose();
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
