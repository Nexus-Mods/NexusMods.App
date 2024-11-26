using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = System.Reactive.Linq.Observable;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    private readonly CollectionRevisionMetadata.ReadOnly _revision;
    private readonly CollectionMetadata.ReadOnly _collection;

    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly NexusModsDataProvider _nexusModsDataProvider;
    private readonly CollectionDownloader _collectionDownloader;

    public CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; }

    public CollectionDownloadViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        CollectionRevisionMetadata.ReadOnly revisionMetadata) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _nexusModsDataProvider = serviceProvider.GetRequiredService<NexusModsDataProvider>();
        _collectionDownloader = new CollectionDownloader(_serviceProvider);

        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var backgroundImagePipeline = ImagePipelines.GetCollectionBackgroundImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

        _revision = revisionMetadata;
        _collection = revisionMetadata.Collection;

        TabTitle = _collection.Name;
        TabIcon = IconValues.Collections;

        TreeDataGridAdapter = new CollectionDownloadTreeDataGridAdapter(_nexusModsDataProvider, revisionMetadata);
        TreeDataGridAdapter.ViewHierarchical.Value = false;

        var requiredDownloadCount = 0;
        var optionalDownloadCount = 0;
        foreach (var file in _revision.Downloads)
        {
            var isOptional = file.IsOptional;

            requiredDownloadCount += isOptional ? 0 : 1;
            optionalDownloadCount += isOptional ? 1 : 0;
        }

        RequiredDownloadsCount = requiredDownloadCount;
        OptionalDownloadsCount = optionalDownloadCount;

        var loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        DownloadAllCommand = loginManager.IsPremiumObservable.ToObservable().ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => _collectionDownloader.DownloadAll(_revision, onlyRequired: true, db: _connection.Db, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        InstallCollectionCommand = new ReactiveCommand<Unit>(canExecuteSource: R3.Observable.Return(false), initialCanExecute: false);

        this.WhenActivated(disposables =>
        {
            TreeDataGridAdapter.Activate();
            Disposable.Create(TreeDataGridAdapter, static adapter => adapter.Deactivate());

            Observable
                .Return(_revision)
                .OffUi()
                .SelectMany(revision => _collectionDownloader.RequiredDownloadedCountObservable(revision))
                .OnUI()
                .Subscribe(count =>
                {
                    if (count == RequiredDownloadsCount)
                    {
                        CollectionStatusText = "Ready to install - All required mods downloaded";
                    }
                    else
                    {
                        CollectionStatusText = $"{count} of {RequiredDownloadsCount} required mods downloaded";
                    }
                })
                .AddTo(disposables);

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
                        f0: x => _collectionDownloader.Download(x, cancellationToken),
                        f1: x => _collectionDownloader.Download(x, cancellationToken)
                    );
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);
        });
    }

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

    [Reactive] public Bitmap? TileImage { get; private set; }
    [Reactive] public Bitmap? BackgroundImage { get; private set; }
    [Reactive] public Bitmap? AuthorAvatar { get; private set; }
    [Reactive] public string CollectionStatusText { get; private set; } = "";

    public ReactiveCommand<Unit> DownloadAllCommand { get; }
    public ReactiveCommand<Unit> InstallCollectionCommand { get; }
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
            ColumnCreator.CreateColumn<ILibraryItemModel, ILibraryItemWithName>(),
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
