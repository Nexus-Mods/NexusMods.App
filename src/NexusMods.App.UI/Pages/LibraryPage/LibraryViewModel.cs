using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Library;
using NexusMods.App.UI.Pages.LibraryPage.Collections;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
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
    public ReactiveCommand<Unit> SwitchViewCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }

    public ReactiveCommand<Unit> RemoveSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> OpenFilePickerCommand { get; }

    public ReactiveCommand<Unit> OpenNexusModsCommand { get; }

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

        _advancedInstaller = serviceProvider.GetRequiredKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller");

        TabTitle = Language.LibraryPageTitle;
        TabIcon = IconValues.LibraryOutline;
        
        _loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = _loadout.InstallationInstance.Game;

        EmptyLibrarySubtitleText = string.Format(Language.FileOriginsPageViewModel_EmptyLibrarySubtitleText, game.Name);

        SwitchViewCommand = new ReactiveCommand<Unit>(_ =>
        {
            Adapter.ViewHierarchical.Value = !Adapter.ViewHierarchical.Value;
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
                var gameDomain = (await _gameIdMappingCache.TryGetDomainAsync(game.GameId, cancellationToken));
                var gameUri = NexusModsUrlBuilder.CreateGenericUri($"https://www.nexusmods.com/{gameDomain}");
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
                    if (message.TryPickT0(out var installMessage, out var updateMessage))
                    {
                        foreach (var id in installMessage.Ids)
                        {
                            var libraryItem = LibraryItem.Load(_connection.Db, id);
                            if (!libraryItem.IsValid()) continue;
                            await InstallLibraryItem(libraryItem, _loadout, cancellationToken);
                        }
                    }
                    else
                    {
                        await HandleUpdateMessage(updateMessage, cancellationToken);
                    }
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);

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

            // Auto check updates on entering library.
            RefreshUpdatesCommand.Execute(Unit.Default);
        });
    }

    private async ValueTask HandleUpdateMessage(UpdateMessage updateMessage, CancellationToken cancellationToken)
    {
        var updatesOnPage = updateMessage.Updates;
        
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
                var modFileUrl = NexusModsUrlBuilder.CreateModFileDownloadUri(file.Uid.FileId, file.Uid.GameId);
                var osInterop = _serviceProvider.GetRequiredService<IOSInterop>();
                await osInterop.OpenUrl(modFileUrl, cancellationToken: cancellationToken);
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

    // Note(sewer): ValueTask because of R3 constraints with ReactiveCommand API
    private async ValueTask RefreshUpdates(CancellationToken token) 
    {
        await _modUpdateService.CheckAndUpdateModPages(token, notify: true);
    }

    private async ValueTask InstallItems(LibraryItemId[] ids, bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var items = ids
            .Select(id => LibraryItem.Load(db, id))
            .Where(x => x.IsValid())
            .ToArray();

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: items.Length,
            body: (i, innerCancellationToken) => InstallLibraryItem(items[i], _loadout, innerCancellationToken, useAdvancedInstaller),
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

    private ValueTask InstallSelectedItems(bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        return InstallItems(GetSelectedIds(), useAdvancedInstaller, cancellationToken);
    }

    private async ValueTask InstallLibraryItem(
        LibraryItem.ReadOnly libraryItem,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken,
        bool useAdvancedInstaller = false)
    {
        await _libraryService.InstallItem(libraryItem, loadout, installer: useAdvancedInstaller ? _advancedInstaller : null);
    }

    private async ValueTask RemoveSelectedItems(CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var toRemove = GetSelectedIds().Select(id => LibraryItem.Load(db, id)).ToArray();
        await LibraryItemRemover.RemoveAsync(_connection, _serviceProvider.GetRequiredService<IOverlayController>(), _libraryService, toRemove, cancellationToken);
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
public readonly record struct UpdateMessage(ModUpdatesOnModPage Updates, CompositeItemModel<EntityId> TreeNode);

public class LibraryTreeDataGridAdapter :
    TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>,
    ITreeDataGirdMessageAdapter<OneOf<InstallMessage, UpdateMessage>>
{
    private readonly ILibraryDataProvider[] _libraryDataProviders;
    private readonly LibraryFilter _libraryFilter;

    public Subject<OneOf<InstallMessage, UpdateMessage>> MessageSubject { get; } = new();

    private readonly IDisposable _activationDisposable;
    private readonly Dictionary<CompositeItemModel<EntityId>, IDisposable> _commandDisposables = new();

    public LibraryTreeDataGridAdapter(IServiceProvider serviceProvider, LibraryFilter libraryFilter)
    {
        _libraryFilter = libraryFilter;
        _libraryDataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();

        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            Disposable.Create(self._commandDisposables,static commandDisposables =>
            {
                foreach (var kv in commandDisposables)
                {
                    var (_, disposable) = kv;
                    disposable.Dispose();
                }

                commandDisposables.Clear();
            }).AddTo(disposables);
        });
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        return _libraryDataProviders.Select(x => x.ObserveLibraryItems(_libraryFilter)).MergeChangeSets();
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        var installActionDisposable = model.SubscribeToComponent<LibraryComponents.InstallAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.InstallComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandInstall.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, _, component) = state;
                var ids = component.ItemIds.ToArray();

                self.MessageSubject.OnNext(new InstallMessage(ids));
            })
        );

        var updateActionDisposable = model.SubscribeToComponent<LibraryComponents.UpdateAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.UpdateComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandUpdate
                .SubscribeOnUIThreadDispatcher() // Update payload may expand row for free users, requiring UI thread.
                .Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, component) = state;
                var newFile = component.NewFiles.Value;
                self.MessageSubject.OnNext(new UpdateMessage(newFile, model));
            })
        );

        var disposable = Disposable.Combine(installActionDisposable, updateActionDisposable);

        var didAdd = _commandDisposables.TryAdd(model, disposable);
        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");
    }

    protected override void BeforeModelDeactivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelDeactivationHook(model);

        var didRemove = _commandDisposables.Remove(model, out var disposable);
        Debug.Assert(didRemove, "subscription for the model should exist");
        disposable?.Dispose();
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, SharedColumns.Name>(sortDirection: ListSortDirection.Ascending);

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, LibraryColumns.ItemVersion>(),
            ColumnCreator.Create<EntityId, LibraryColumns.ItemSize>(),
            ColumnCreator.Create<EntityId, LibraryColumns.DownloadedDate>(),
            ColumnCreator.Create<EntityId, SharedColumns.InstalledDate>(),
            ColumnCreator.Create<EntityId, LibraryColumns.Actions>(),
        ];
    }

    private bool _isDisposed;

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(_activationDisposable, MessageSubject);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
