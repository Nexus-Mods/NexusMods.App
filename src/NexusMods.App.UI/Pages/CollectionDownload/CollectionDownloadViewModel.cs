using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
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

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    private readonly CollectionRevisionMetadata.ReadOnly _revision;
    private readonly CollectionMetadata.ReadOnly _collection;

    private readonly IServiceProvider _serviceProvider;
    private readonly NexusModsDataProvider _nexusModsDataProvider;
    private readonly CollectionDownloader _collectionDownloader;

    public CollectionDownloadTreeDataGridAdapter RequiredDownloadsAdapter { get; }

    public CollectionDownloadTreeDataGridAdapter OptionalDownloadsAdapter { get; }

    public CollectionDownloadViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        CollectionRevisionMetadata.ReadOnly revisionMetadata) : base(windowManager)
    {
        TabTitle = _collection.Name;
        TabIcon = IconValues.Collections;

        _serviceProvider = serviceProvider;
        _nexusModsDataProvider = serviceProvider.GetRequiredService<NexusModsDataProvider>();
        _collectionDownloader = new CollectionDownloader(_serviceProvider);

        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var backgroundImagePipeline = ImagePipelines.GetCollectionBackgroundImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

        _revision = revisionMetadata;
        _collection = revisionMetadata.Collection;

        RequiredDownloadsAdapter = new CollectionDownloadTreeDataGridAdapter(_nexusModsDataProvider, revisionMetadata);
        RequiredDownloadsAdapter.ViewHierarchical.Value = false;
        OptionalDownloadsAdapter = new CollectionDownloadTreeDataGridAdapter(_nexusModsDataProvider, revisionMetadata);
        OptionalDownloadsAdapter.ViewHierarchical.Value = false;

        // TODO:
        CollectionStatusText = "TODO";

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

        this.WhenActivated(disposables =>
        {
            RequiredDownloadsAdapter.Activate();
            OptionalDownloadsAdapter.Activate();
            Disposable.Create(RequiredDownloadsAdapter, static adapter => adapter.Deactivate());
            Disposable.Create(OptionalDownloadsAdapter, static adapter => adapter.Deactivate());

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

            _nexusModsDataProvider
                .ObserveCollectionItems(revisionMetadata)
                .SubscribeWithErrorLogging()
                .AddTo(disposables);

            RequiredDownloadsAdapter.MessageSubject.SubscribeAwait(
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

            OptionalDownloadsAdapter.MessageSubject.SubscribeAwait(
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
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public string AuthorName => _collection.Author.Name;
    public bool IsAdult => _revision.IsAdult;
    public CollectionSlug Slug => _collection.Slug;
    public RevisionNumber RevisionNumber => _revision.RevisionNumber;

    public int RequiredDownloadsCount { get; }
    public int OptionalDownloadsCount { get; }

    [Reactive] public Bitmap? TileImage { get; private set; }
    [Reactive] public Bitmap? BackgroundImage { get; private set; }
    [Reactive] public Bitmap? AuthorAvatar { get; private set; }
    [Reactive] public string CollectionStatusText { get; private set; }
}

public record DownloadMessage(DownloadableItem Item);

public class CollectionDownloadTreeDataGridAdapter : TreeDataGridAdapter<ILibraryItemModel, EntityId>,
    ITreeDataGirdMessageAdapter<DownloadMessage>
{
    private readonly NexusModsDataProvider _nexusModsDataProvider;
    private readonly CollectionRevisionMetadata.ReadOnly _revisionMetadata;

    public Subject<DownloadMessage> MessageSubject { get; } = new();

    private readonly Dictionary<ILibraryItemModel, IDisposable> _commandDisposables = new();
    private readonly IDisposable _activationDisposable;

    public CollectionDownloadTreeDataGridAdapter(NexusModsDataProvider nexusModsDataProvider, CollectionRevisionMetadata.ReadOnly revisionMetadata)
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
        return _nexusModsDataProvider.ObserveCollectionItems(_revisionMetadata);
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
