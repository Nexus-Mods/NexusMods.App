using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Aggregation;
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
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using OneOf;
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

    private readonly IServiceProvider _serviceProvider;
    private readonly LoadoutId _targetLoadout;

    public CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; }

    public CollectionDownloadViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        LoadoutId targetLoadout) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var nexusModsDataProvider = serviceProvider.GetRequiredService<NexusModsDataProvider>();
        var mappingCache = serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();
        var osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var collectionDownloader = new CollectionDownloader(serviceProvider);
        var loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        var overlayController = serviceProvider.GetRequiredService<IOverlayController>();
        var jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();

        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var backgroundImagePipeline = ImagePipelines.GetCollectionBackgroundImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

        _revision = revisionMetadata;
        _collection = revisionMetadata.Collection;
        _targetLoadout = targetLoadout;

        var libraryFile = collectionDownloader.GetLibraryFile(revisionMetadata);
        var collectionJsonFile = nexusModsLibrary.GetCollectionJsonFile(libraryFile);

        TabTitle = _collection.Name;
        TabIcon = IconValues.CollectionsOutline;

        TreeDataGridAdapter = new CollectionDownloadTreeDataGridAdapter(nexusModsDataProvider, revisionMetadata, targetLoadout);
        TreeDataGridAdapter.ViewHierarchical.Value = false;

        RequiredDownloadsCount = CollectionDownloader.CountItems(_revision, CollectionDownloader.ItemType.Required);
        OptionalDownloadsCount = CollectionDownloader.CountItems(_revision, CollectionDownloader.ItemType.Optional);

        CommandDownloadRequiredItems = _isDownloadingRequiredItems.CombineLatest(_canDownloadRequiredItems, static (isDownloading, canDownload) => !isDownloading && canDownload).ToReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) =>
            {
                if (loginManager.IsPremium) await collectionDownloader.DownloadItems(_revision, itemType: CollectionDownloader.ItemType.Required, db: connection.Db, cancellationToken: cancellationToken);
                else overlayController.Enqueue(serviceProvider.GetRequiredService<IUpgradeToPremiumViewModel>());
            },
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        CommandDownloadOptionalItems = _isDownloadingOptionalItems.CombineLatest(_canDownloadOptionalItems, static (isDownloading, canDownload) => !isDownloading && canDownload).ToReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) =>
            {
                if (loginManager.IsPremium) await collectionDownloader.DownloadItems(_revision, itemType: CollectionDownloader.ItemType.Optional, db: connection.Db, cancellationToken: cancellationToken);
                else overlayController.Enqueue(serviceProvider.GetRequiredService<IUpgradeToPremiumViewModel>());
            },
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        // TODO: implement this button
        CommandInstallOptionalItems = IsInstalling.CombineLatest(_canInstallOptionalItems, static (isInstalling, canInstall) => !isInstalling && canInstall).ToReactiveCommand<Unit>();

        CommandInstallRequiredItems = IsInstalling.CombineLatest(_canInstallRequiredItems, static (isInstalling, canInstall) => !isInstalling && canInstall).ToReactiveCommand<Unit>(
            executeAsync: async (_, _) => { await InstallCollectionJob.Create(
                serviceProvider,
                targetLoadout,
                source: libraryFile,
                revisionMetadata,
                items: CollectionDownloader.GetItems(revisionMetadata, CollectionDownloader.ItemType.Required)
            ); },
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        CommandDeleteCollectionRevision = new ReactiveCommand(
            executeAsync: async (_, _) =>
            {
                var pageData = new PageData
                {
                    FactoryId = LibraryPageFactory.StaticId,
                    Context = new LibraryPageContext()
                    {
                        LoadoutId = targetLoadout,
                    },
                };

                var workspaceController = GetWorkspaceController();
                var behavior = new OpenPageBehavior.ReplaceTab(PanelId, TabId);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);

                await collectionDownloader.DeleteCollectionLoadoutGroup(_revision, cancellationToken: CancellationToken.None);
                await collectionDownloader.DeleteRevision(_revision);
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

        CommandViewCollection = IsInstalled.ToReactiveCommand<NavigationInformation>(info =>
        {
            var group = CollectionDownloader.GetCollectionGroup(_revision, _targetLoadout, connection.Db).Value;

            var pageData = new PageData
            {
                FactoryId = CollectionLoadoutPageFactory.StaticId,
                Context = new CollectionLoadoutPageContext
                {
                    LoadoutId = _targetLoadout,
                    GroupId = group.AsCollectionGroup(),
                },
            };

            var workspaceController = GetWorkspaceController();
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            workspaceController.OpenPage(WorkspaceId, pageData, behavior);
        });

        IsDownloading = _isDownloadingRequiredItems.CombineLatest(_isDownloadingOptionalItems, static (a, b) => a || b).ToBindableReactiveProperty();
        IsUpdateAvailable = NewestRevisionNumber.Select(static optional => optional.HasValue).ToBindableReactiveProperty();

        CommandUpdateCollection = IsUpdateAvailable.ToReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) =>
            {
                var newestRevisionNumber = NewestRevisionNumber.Value.Value;
                var revision = await collectionDownloader.GetOrAddRevision(_collection.Slug, newestRevisionNumber, cancellationToken);

                var pageData = new PageData
                {
                    FactoryId = CollectionDownloadPageFactory.StaticId,
                    Context = new CollectionDownloadPageContext
                    {
                        TargetLoadout = targetLoadout,
                        CollectionRevisionMetadataId = revision,
                    },
                };

                var workspaceController = GetWorkspaceController();
                workspaceController.OpenPage(WorkspaceId, pageData, new OpenPageBehavior.ReplaceTab(PanelId, TabId));
            }, awaitOperation: AwaitOperation.Drop, configureAwait: false
        );

        this.WhenActivated(disposables =>
        {
            TreeDataGridAdapter.Activate().AddTo(disposables);

            jobMonitor
                .HasActiveJob<InstallCollectionJob>(job => job.RevisionMetadata.Id == _revision.Id)
                .OnUI()
                .Subscribe(isInstalling => IsInstalling.Value = isInstalling)
                .AddTo(disposables);

            jobMonitor
                .HasActiveJob<DownloadCollectionJob>(job => job.RevisionMetadata.Id == _revision.Id && job.ItemType == CollectionDownloader.ItemType.Required)
                .OnUI()
                .Subscribe(isDownloading => _isDownloadingRequiredItems.OnNext(isDownloading))
                .AddTo(disposables);

            jobMonitor
                .HasActiveJob<DownloadCollectionJob>(job => job.RevisionMetadata.Id == _revision.Id && job.ItemType == CollectionDownloader.ItemType.Optional)
                .OnUI()
                .Subscribe(isDownloading => _isDownloadingOptionalItems.OnNext(isDownloading))
                .AddTo(disposables);

            var numDownloadedRequiredItemsObservable = Observable
                .Return(_revision)
                .OffUi()
                .SelectMany(revision => collectionDownloader.DownloadedItemCountObservable(revision, itemType: CollectionDownloader.ItemType.Required));

            var numDownloadedOptionalItemsObservable = Observable
                .Return(_revision)
                .OffUi()
                .SelectMany(revision => collectionDownloader.DownloadedItemCountObservable(revision, itemType: CollectionDownloader.ItemType.Optional));

            loginManager.IsPremiumObservable
                .Prepend(false)
                .OnUI()
                .Subscribe(isPremium => CanDownloadAutomatically = isPremium)
                .AddTo(disposables);

            var collectionGroupObservable = collectionDownloader.GetCollectionGroupObservable(_revision, _targetLoadout);
            var isCollectionInstalledObservable = collectionDownloader
                .IsCollectionInstalledObservable(_revision, collectionGroupObservable)
                .Prepend(false);

            numDownloadedRequiredItemsObservable.CombineLatest(isCollectionInstalledObservable)
                .OnUI()
                .Subscribe(tuple =>
                {
                    var (numDownloadedRequiredItems, isCollectionInstalled) = tuple;
                    var hasDownloadedAllRequiredItems = numDownloadedRequiredItems == RequiredDownloadsCount;

                    CountDownloadedRequiredItems = numDownloadedRequiredItems;
                    _canInstallRequiredItems.OnNext(!isCollectionInstalled && hasDownloadedAllRequiredItems);
                    _canDownloadRequiredItems.OnNext(!hasDownloadedAllRequiredItems);

                    if (hasDownloadedAllRequiredItems)
                    {
                        if (isCollectionInstalled)
                        {
                            IsInstalled.Value = true;
                            CollectionStatusText = Language.CollectionDownloadViewModel_CollectionDownloadViewModel_Ready_to_play___All_required_mods_installed;
                        }
                        else
                        {
                            CollectionStatusText = Language.CollectionDownloadViewModel_Ready_to_install;
                        }
                    }
                    else
                    {
                        CollectionStatusText = string.Format(Language.CollectionDownloadViewModel_Num_required_mods_downloaded, numDownloadedRequiredItems, RequiredDownloadsCount);
                    }
                }).AddTo(disposables);

            numDownloadedOptionalItemsObservable
                .OnUI()
                .Subscribe(numDownloadedOptionalItems =>
                {
                    var hasDownloadedAllOptionalItems = numDownloadedOptionalItems == OptionalDownloadsCount;

                    CountDownloadedOptionalItems = numDownloadedOptionalItems;
                    _canInstallOptionalItems.OnNext(numDownloadedOptionalItems > 0);
                    _canDownloadOptionalItems.OnNext(!hasDownloadedAllOptionalItems);
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
                    return message.Match(
                        f0: download => download.Item.Match(
                            f0: x => collectionDownloader.Download(x, cancellationToken),
                            f1: x => collectionDownloader.Download(x, cancellationToken)
                        ),
                        f1: install => InstallItem(install.Item, cancellationToken)
                    );
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);

            R3.Observable.Return(_revision)
                .ObserveOnThreadPool()
                .SelectAwait((revision, cancellationToken) => nexusModsLibrary.GetNewerRevisionNumbers(revision, cancellationToken))
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (newerRevisions, self) =>
                {
                    self.IsUpdateAvailable.Value = newerRevisions.Length > 0;
                    self.NewestRevisionNumber.Value = newerRevisions.First();
                }).AddTo(disposables);
        });
    }

    private async ValueTask InstallItem(NexusMods.Abstractions.NexusModsLibrary.Models.CollectionDownload.ReadOnly download, CancellationToken cancellationToken)
    {
        var monitor = _serviceProvider.GetRequiredService<IJobMonitor>();

        var job = await InstallCollectionDownloadJob.Create(
            serviceProvider: _serviceProvider,
            targetLoadout: _targetLoadout,
            download: download,
            cancellationToken: cancellationToken
        );

        await monitor.Begin<InstallCollectionDownloadJob, LoadoutItemGroup.ReadOnly>(job);
    }

    public BindableReactiveProperty<bool> IsInstalled { get; } = new(value: false);

    private readonly BehaviorSubject<bool> _canDownloadRequiredItems = new(initialValue: false);
    private readonly BehaviorSubject<bool> _canDownloadOptionalItems = new(initialValue: false);
    private readonly BehaviorSubject<bool> _isDownloadingRequiredItems = new(initialValue: false);
    private readonly BehaviorSubject<bool> _isDownloadingOptionalItems = new(initialValue: false);
    public BindableReactiveProperty<bool> IsDownloading { get; }

    private readonly BehaviorSubject<bool> _canInstallRequiredItems = new(initialValue: false);
    private readonly BehaviorSubject<bool> _canInstallOptionalItems = new(initialValue: false);
    public BindableReactiveProperty<bool> IsInstalling { get; } = new(value: false);

    public BindableReactiveProperty<bool> IsUpdateAvailable { get; }
    public BindableReactiveProperty<Optional<RevisionNumber>> NewestRevisionNumber { get; } = new();

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

    [Reactive] public bool CanDownloadAutomatically { get; private set; }

    public ReactiveCommand<NavigationInformation> CommandViewCollection { get; }
    public ReactiveCommand<Unit> CommandDownloadRequiredItems { get; }
    public ReactiveCommand<Unit> CommandInstallRequiredItems { get; }
    public ReactiveCommand<Unit> CommandDownloadOptionalItems { get; }
    public ReactiveCommand<Unit> CommandInstallOptionalItems { get; }
    public ReactiveCommand<Unit> CommandUpdateCollection { get; }

    public ReactiveCommand<Unit> CommandViewOnNexusMods { get; }
    public ReactiveCommand<Unit> CommandOpenJsonFile { get; }
    public ReactiveCommand<Unit> CommandDeleteAllDownloads { get; }
    public ReactiveCommand<Unit> CommandDeleteCollectionRevision { get; }
}

public record DownloadMessage(DownloadableItem Item);

public record InstallMessage(NexusMods.Abstractions.NexusModsLibrary.Models.CollectionDownload.ReadOnly Item);

public class Message : OneOfBase<DownloadMessage, InstallMessage>
{
    public Message(OneOf<DownloadMessage, InstallMessage> input) : base(input) { }
}

public class CollectionDownloadTreeDataGridAdapter : TreeDataGridAdapter<ILibraryItemModel, EntityId>,
    ITreeDataGirdMessageAdapter<Message>
{
    private readonly NexusModsDataProvider _nexusModsDataProvider;
    private readonly CollectionRevisionMetadata.ReadOnly _revisionMetadata;
    private readonly LoadoutId _targetLoadout;

    public Subject<Message> MessageSubject { get; } = new();
    public R3.ReactiveProperty<CollectionDownloadsFilter> Filter { get; } = new(value: CollectionDownloadsFilter.OnlyRequired);

    private readonly Dictionary<ILibraryItemModel, IDisposable> _commandDisposables = new();
    private readonly IDisposable _activationDisposable;

    public CollectionDownloadTreeDataGridAdapter(
        NexusModsDataProvider nexusModsDataProvider,
        CollectionRevisionMetadata.ReadOnly revisionMetadata,
        LoadoutId targetLoadout)
    {
        _nexusModsDataProvider = nexusModsDataProvider;
        _revisionMetadata = revisionMetadata;
        _targetLoadout = targetLoadout;

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
                subject.OnNext(new Message(payload));
            });

            var didAdd = _commandDisposables.TryAdd(model, disposable);
            Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");
        } else if (model is NexusModsFileMetadataLibraryItemModel.Installable withInstallAction)
        {
            var disposable = withInstallAction.InstallItemCommand.Subscribe((MessageSubject, withInstallAction), static (_, state) =>
            {
                var (subject, withInstallAction) = state;
                subject.OnNext(new Message(new InstallMessage(withInstallAction.Download.AsCollectionDownload())));
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
        return _nexusModsDataProvider.ObserveCollectionItems(_revisionMetadata, Filter.AsSystemObservable(), _targetLoadout);
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
