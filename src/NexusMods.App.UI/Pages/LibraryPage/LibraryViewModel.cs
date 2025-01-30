using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
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
    private readonly INexusApiClient _nexusApiClient;
    private readonly ILibraryItemInstaller _advancedInstaller;
    private readonly IGameDomainToGameIdMappingCache _gameIdMappingCache;
    private readonly Loadout.ReadOnly _loadout;
    private readonly IModUpdateService _modUpdateService;

    public LibraryTreeDataGridAdapter Adapter { get; }
    private ReadOnlyObservableCollection<ICollectionCardViewModel> _collections = new([]);
    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections => _collections;

    private BehaviorSubject<Optional<LoadoutId>> LoadoutSubject { get; } = new(Optional<LoadoutId>.None);

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        IGameDomainToGameIdMappingCache gameIdMappingCache,
        INexusApiClient nexusApiClient,
        LoadoutId loadoutId) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _nexusApiClient = nexusApiClient;
        _gameIdMappingCache = gameIdMappingCache;
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _modUpdateService = serviceProvider.GetRequiredService<IModUpdateService>();

        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

        var ticker = R3.Observable
            .Interval(period: TimeSpan.FromSeconds(30), timeProvider: ObservableSystem.DefaultTimeProvider)
            .ObserveOnUIThreadDispatcher()
            .Select(_ => TimeProvider.System.GetLocalNow())
            .Publish(initialValue: TimeProvider.System.GetLocalNow());

        var loadoutObservable = LoadoutSubject
            .Where(static id => id.HasValue)
            .Select(static id => id.Value)
            .AsSystemObservable()
            .Replay(bufferSize: 1);

        var gameObservable = loadoutObservable
            .Select(id => Loadout.Load(_connection.Db, id).InstallationInstance.Game)
            .Replay(bufferSize: 1);

        var libraryFilter = new LibraryFilter(
            loadoutObservable: loadoutObservable,
            gameObservable: gameObservable
        );

        Adapter = new LibraryTreeDataGridAdapter(serviceProvider, libraryFilter);
        LoadoutSubject.OnNext(loadoutId);

        _advancedInstaller = serviceProvider.GetRequiredKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller");

        TabTitle = Language.LibraryPageTitle;
        TabIcon = IconValues.LibraryOutline;

        ticker.Connect();

        _loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = _loadout.InstallationInstance.Game;

        EmptyLibrarySubtitleText = string.Format(Language.FileOriginsPageViewModel_EmptyLibrarySubtitleText, game.Name);

        SwitchViewCommand = new ReactiveCommand<Unit>(_ =>
        {
            Adapter.ViewHierarchical.Value = !Adapter.ViewHierarchical.Value;
        });

        RefreshUpdatesCommand = new ReactiveCommand<Unit>(
            executeAsync: (_, token) => RefreshUpdates(token),
            awaitOperation: AwaitOperation.Sequential
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
            disposables.Add(loadoutObservable.Connect());
            disposables.Add(gameObservable.Connect());

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
                        await HandleUpdateMessage(updateMessage.NewFile, cancellationToken);
                    }
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);

            CollectionRevisionMetadata.ObserveAll(_connection)
                .FilterImmutable(revision => revision.Collection.GameId == game.GameId)
                .OnUI()
                .Transform(ICollectionCardViewModel (revision) => new CollectionCardViewModel(
                    tileImagePipeline: tileImagePipeline,
                    userAvatarPipeline: userAvatarPipeline,
                    windowManager: WindowManager,
                    workspaceId: WorkspaceId,
                    connection: _connection,
                    revision: revision.RevisionId,
                    targetLoadout: _loadout)
                )
                .Bind(out _collections)
                .Subscribe()
                .AddTo(disposables);
        });
    }

    private async ValueTask HandleUpdateMessage(NexusModsFileMetadata.ReadOnly fileMetadata, CancellationToken cancellationToken)
    {
        var modFileUrl = NexusModsUrlBuilder.CreateModFileDownloadUri(fileMetadata.Uid.FileId, fileMetadata.Uid.GameId);
        var osInterop = _serviceProvider.GetRequiredService<IOSInterop>();
        await osInterop.OpenUrl(modFileUrl, cancellationToken: cancellationToken);
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
public readonly record struct UpdateMessage(NexusModsFileMetadata.ReadOnly NewFile);

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
            factory: static (self, itemModel, component) => component.CommandUpdate.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, _, component) = state;
                var newFile = component.NewFile.Value;

                self.MessageSubject.OnNext(new UpdateMessage(newFile));
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
