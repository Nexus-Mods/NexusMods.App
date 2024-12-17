using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LibraryPage.Collections;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = System.Reactive.Linq.Observable;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public sealed class CollectionDownloadViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    private readonly CollectionRevisionMetadata.ReadOnly _revision;
    private readonly CollectionMetadata.ReadOnly _collection;

    public CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; }

    public CollectionDownloadViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        LoadoutId targetLoadout) : base(windowManager)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var nexusModsDataProvider = serviceProvider.GetRequiredService<NexusModsDataProvider>();
        var mappingCache = serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();
        var osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var collectionDownloader = new CollectionDownloader(serviceProvider);
        var loginManager = serviceProvider.GetRequiredService<ILoginManager>();

        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var backgroundImagePipeline = ImagePipelines.GetCollectionBackgroundImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

        _revision = revisionMetadata;
        _collection = revisionMetadata.Collection;

        var libraryFile = collectionDownloader.GetLibraryFile(revisionMetadata);
        var collectionJsonFile = nexusModsLibrary.GetCollectionJsonFile(libraryFile);

        TabTitle = _collection.Name;
        TabIcon = IconValues.Collections;

        TreeDataGridAdapter = new CollectionDownloadTreeDataGridAdapter(nexusModsDataProvider, revisionMetadata);
        TreeDataGridAdapter.ViewHierarchical.Value = false;

        RequiredDownloadsCount = collectionDownloader.CountItems(_revision, CollectionDownloader.ItemType.Required);
        OptionalDownloadsCount = collectionDownloader.CountItems(_revision, CollectionDownloader.ItemType.Optional);

        CommandDownloadRequiredItems = _canDownloadRequiredItems.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => collectionDownloader.DownloadItems(_revision, itemType: CollectionDownloader.ItemType.Required, db: connection.Db, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        CommandDownloadOptionalItems = _canDownloadOptionalItems.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => collectionDownloader.DownloadItems(_revision, itemType: CollectionDownloader.ItemType.Optional, db: connection.Db, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        // TODO: implement this button
        CommandInstallOptionalItems = _canInstallOptionalItems.ToReactiveCommand<Unit>();

        CommandInstallRequiredItems = _canInstallRequiredItems.ToReactiveCommand<Unit>(
            executeAsync: async (_, _) => { await InstallCollectionJob.Create(
                serviceProvider,
                targetLoadout,
                source: libraryFile,
                revisionMetadata,
                items: collectionDownloader.GetItems(revisionMetadata, CollectionDownloader.ItemType.Required),
                group: Optional<NexusCollectionLoadoutGroup.ReadOnly>.None
            ); },
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        CommandDeleteCollection = new ReactiveCommand(
            executeAsync: async (_, cancellationToken) =>
            {
                var pageData = new PageData
                {
                    FactoryId = CollectionsPageFactory.StaticId,
                    Context = new CollectionsPageContext
                    {
                        LoadoutId = targetLoadout,
                    },
                };

                await collectionDownloader.DeleteCollectionLoadoutGroup(_revision, cancellationToken);

                var workspaceController = GetWorkspaceController();
                var behavior = new OpenPageBehavior.ReplaceTab(PanelId, TabId);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);

                await nexusModsLibrary.DeleteCollection(_collection, cancellationToken);
            },
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false,
            cancelOnCompleted: false
        );

        CommandDeleteAllDownloads = new ReactiveCommand(canExecuteSource: R3.Observable.Return(false), initialCanExecute: false);

        CommandViewOnNexusMods = new ReactiveCommand(
            executeAsync: async (_, cancellationToken) =>
            {
                var gameDomain = await mappingCache.TryGetDomainAsync(_collection.GameId, cancellationToken);
                if (!gameDomain.HasValue) throw new NotSupportedException($"Expected a valid game domain for `{_collection.GameId}`");

                var uri = _collection.GetUri(gameDomain.Value);
                await osInterop.OpenUrl(uri, logOutput: false, fireAndForget: true, cancellationToken: cancellationToken);
            },
            awaitOperation: AwaitOperation.Sequential,
            configureAwait: false
        );

        CommandViewInLibrary = new ReactiveCommand(canExecuteSource: R3.Observable.Return(false), initialCanExecute: false);

        CommandOpenJsonFile = new ReactiveCommand(
            execute: _ =>
            {
                var pageData = new PageData
                {
                    FactoryId = TextEditorPageFactory.StaticId,
                    Context = new TextEditorPageContext
                    {
                        FileId = collectionJsonFile.AsLibraryFile().LibraryFileId,
                        FilePath = collectionJsonFile.AsLibraryFile().FileName,
                    },
                };

                var workspaceController = GetWorkspaceController();
                var behavior = new OpenPageBehavior.NewTab(PanelId);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
            }
        );

        this.WhenActivated(disposables =>
        {
            TreeDataGridAdapter.Activate();
            Disposable.Create(TreeDataGridAdapter, static adapter => adapter.Deactivate()).AddTo(disposables);

            var numDownloadedRequiredItemsObservable = Observable
                .Return(_revision)
                .OffUi()
                .SelectMany(revision => collectionDownloader.DownloadedItemCountObservable(revision, itemType: CollectionDownloader.ItemType.Required));

            var numDownloadedOptionalItemsObservable = Observable
                .Return(_revision)
                .OffUi()
                .SelectMany(revision => collectionDownloader.DownloadedItemCountObservable(revision, itemType: CollectionDownloader.ItemType.Optional));

            var isPremiumObservable = loginManager.IsPremiumObservable.Prepend(false);
            var isCollectionInstalledObservable = collectionDownloader.IsCollectionInstalled(_revision).Prepend(false);

            numDownloadedRequiredItemsObservable.CombineLatest(isPremiumObservable, isCollectionInstalledObservable)
                .OnUI()
                .Subscribe(tuple =>
                {
                    var (numDownloadedRequiredItems, isPremium, isCollectionInstalled) = tuple;
                    var hasDownloadedAllRequiredItems = numDownloadedRequiredItems == RequiredDownloadsCount;

                    CountDownloadedRequiredItems = numDownloadedRequiredItems;
                    _canInstallRequiredItems.OnNext(!isCollectionInstalled && hasDownloadedAllRequiredItems);
                    _canDownloadRequiredItems.OnNext(!hasDownloadedAllRequiredItems && isPremium);

                    if (hasDownloadedAllRequiredItems)
                    {
                        CollectionStatusText = Language.CollectionDownloadViewModel_Ready_to_install;
                    }
                    else
                    {
                        CollectionStatusText = string.Format(Language.CollectionDownloadViewModel_Num_required_mods_downloaded, numDownloadedRequiredItems, RequiredDownloadsCount);
                    }
                }).AddTo(disposables);

            numDownloadedOptionalItemsObservable.CombineLatest(isPremiumObservable)
                .OnUI()
                .Subscribe(tuple =>
                {
                    var (numDownloadedOptionalItems, isPremium) = tuple;
                    var hasDownloadedAllOptionalItems = numDownloadedOptionalItems == OptionalDownloadsCount;

                    CountDownloadedOptionalItems = numDownloadedOptionalItems;
                    _canInstallOptionalItems.OnNext(numDownloadedOptionalItems > 0);
                    _canDownloadOptionalItems.OnNext(!hasDownloadedAllOptionalItems && isPremium);
                }).AddTo(disposables);

            ImagePipelines.CreateObservable(_collection.Id, tileImagePipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.TileImage = bitmap)
                .AddTo(disposables);

            ImagePipelines.CreateObservable(_collection.Id, backgroundImagePipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.BackgroundImage = bitmap)
                .AddTo(disposables);

            ImagePipelines.CreateObservable(_collection.Author.Id, userAvatarPipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.AuthorAvatar = bitmap)
                .AddTo(disposables);

            TreeDataGridAdapter.MessageSubject.SubscribeAwait(
                onNextAsync: (message, cancellationToken) =>
                {
                    return message.Item.Match(
                        f0: x => collectionDownloader.Download(x, cancellationToken),
                        f1: x => collectionDownloader.Download(x, cancellationToken)
                    );
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);
        });
    }

    private readonly BehaviorSubject<bool> _canDownloadRequiredItems = new(initialValue: false);
    private readonly BehaviorSubject<bool> _canDownloadOptionalItems = new(initialValue: false);
    private readonly BehaviorSubject<bool> _canInstallRequiredItems = new(initialValue: false);
    private readonly BehaviorSubject<bool> _canInstallOptionalItems = new(initialValue: false);

    public string Name => _collection.Name;
    public string Summary => _collection.Summary;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong TotalDownloads => _collection.TotalDownloads;
    public string Category => _collection.Category.Name;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating.ValueOr(0));

    public string AuthorName => _collection.Author.Name;
    public bool IsAdult => _revision.IsAdult;
    public CollectionSlug Slug => _collection.Slug;
    public RevisionNumber RevisionNumber => _revision.RevisionNumber;

    public int RequiredDownloadsCount { get; }
    public int OptionalDownloadsCount { get; }
    [Reactive] public int CountDownloadedOptionalItems { get; private set; }
    [Reactive] public int CountDownloadedRequiredItems { get; private set; }

    [Reactive] public Bitmap? TileImage { get; private set; }
    [Reactive] public Bitmap? BackgroundImage { get; private set; }
    [Reactive] public Bitmap? AuthorAvatar { get; private set; }
    [Reactive] public string CollectionStatusText { get; private set; } = "";

    public ReactiveCommand<Unit> CommandDownloadRequiredItems { get; }
    public ReactiveCommand<Unit> CommandInstallRequiredItems { get; }
    public ReactiveCommand<Unit> CommandDownloadOptionalItems { get; }
    public ReactiveCommand<Unit> CommandInstallOptionalItems { get; }

    public ReactiveCommand<Unit> CommandViewOnNexusMods { get; }
    public ReactiveCommand<Unit> CommandViewInLibrary { get; }
    public ReactiveCommand<Unit> CommandOpenJsonFile { get; }
    public ReactiveCommand<Unit> CommandDeleteAllDownloads { get; }
    public ReactiveCommand<Unit> CommandDeleteCollection { get; }
}

public record DownloadMessage(DownloadableItem Item);

public class CollectionDownloadTreeDataGridAdapter : TreeDataGridAdapter<ILibraryItemModel, EntityId>,
    ITreeDataGirdMessageAdapter<DownloadMessage>
{
    private readonly NexusModsDataProvider _nexusModsDataProvider;
    private readonly CollectionRevisionMetadata.ReadOnly _revisionMetadata;

    public Subject<DownloadMessage> MessageSubject { get; } = new();
    public R3.ReactiveProperty<CollectionDownloadsFilter> Filter { get; } = new(value: CollectionDownloadsFilter.OnlyRequired);

    private readonly Dictionary<ILibraryItemModel, IDisposable> _commandDisposables = new();
    private readonly IDisposable _activationDisposable;

    public CollectionDownloadTreeDataGridAdapter(
        NexusModsDataProvider nexusModsDataProvider,
        CollectionRevisionMetadata.ReadOnly revisionMetadata)
    {
        _nexusModsDataProvider = nexusModsDataProvider;
        _revisionMetadata = revisionMetadata;

        _activationDisposable = this.WhenActivated(static (adapter, disposables) =>
        {
            Disposable.Create(adapter._commandDisposables, static commandDisposables =>
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

    protected override void BeforeModelActivationHook(ILibraryItemModel model)
    {
        if (model is ILibraryItemWithDownloadAction withDownloadAction)
        {
            var disposable = withDownloadAction.DownloadItemCommand.Subscribe(MessageSubject, static (downloadableItem, subject) =>
            {
                var payload = new DownloadMessage(downloadableItem);
                subject.OnNext(payload);
            });

            var didAdd = _commandDisposables.TryAdd(model, disposable);
            Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");
        }

        base.BeforeModelActivationHook(model);
    }

    protected override void BeforeModelDeactivationHook(ILibraryItemModel model)
    {
        if (model is ILibraryItemWithAction)
        {
            var didRemove = _commandDisposables.Remove(model, out var disposable);
            Debug.Assert(didRemove, "subscription for the model should exist");
            disposable?.Dispose();
        }

        base.BeforeModelDeactivationHook(model);
    }

    protected override IObservable<IChangeSet<ILibraryItemModel, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        return _nexusModsDataProvider.ObserveCollectionItems(_revisionMetadata, Filter.AsSystemObservable());
    }

    protected override IColumn<ILibraryItemModel>[] CreateColumns(bool viewHierarchical)
    {
        return
        [
            ColumnCreator.CreateColumn<ILibraryItemModel, ILibraryItemWithThumbnailAndName>(),
            ColumnCreator.CreateColumn<ILibraryItemModel, ILibraryItemWithVersion>(),
            ColumnCreator.CreateColumn<ILibraryItemModel, ILibraryItemWithSize>(),
            ColumnCreator.CreateColumn<ILibraryItemModel, ILibraryItemWithAction>(),
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
