using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    private readonly CollectionRevisionMetadata.ReadOnly _revision;
    private readonly CollectionMetadata.ReadOnly _collection;
    private readonly NexusModsDataProvider _nexusModsDataProvider;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly ILibraryService _libraryService;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IOSInterop _osInterop;
    private readonly ILoginManager _loginManager;

    public CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; }

    public CollectionDownloadViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        CollectionRevisionMetadata.ReadOnly revisionMetadata) : base(windowManager)
    {
        _nexusModsDataProvider = serviceProvider.GetRequiredService<NexusModsDataProvider>();
        _nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        _loginManager = serviceProvider.GetRequiredService<ILoginManager>();

        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var backgroundImagePipeline = ImagePipelines.GetCollectionBackgroundImagePipeline(serviceProvider);

        _revision = revisionMetadata;
        _collection = revisionMetadata.Collection;
        TreeDataGridAdapter = new CollectionDownloadTreeDataGridAdapter(_nexusModsDataProvider, revisionMetadata);
        TreeDataGridAdapter.ViewHierarchical.Value = false;

        // TODO:
        CollectionStatusText = "TODO";

        var requiredModCount = 0;
        var optionalModCount = 0;
        foreach (var file in _revision.Downloads)
        {
            var isOptional = file.IsOptional;

            requiredModCount += isOptional ? 0 : 1;
            optionalModCount += isOptional ? 1 : 0;
        }

        RequiredModCount = requiredModCount;
        OptionalModCount = optionalModCount;

        this.WhenActivated(disposables =>
        {
            TreeDataGridAdapter.Activate();
            Disposable.Create(TreeDataGridAdapter, static adapter => adapter.Deactivate());

            ImagePipelines.CreateObservable(_collection.Id, tileImagePipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.TileImage = bitmap)
                .AddTo(disposables);

            ImagePipelines.CreateObservable(_collection.Id, backgroundImagePipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.BackgroundImage = bitmap)
                .AddTo(disposables);

            _nexusModsDataProvider
                .ObserveCollectionItems(revisionMetadata)
                .SubscribeWithErrorLogging()
                .AddTo(disposables);

            TreeDataGridAdapter.MessageSubject.SubscribeAwait(
                onNextAsync: (message, cancellationToken) => DownloadOrOpenPage(message.Item.AsT0, cancellationToken),
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);
        });
    }

    private async ValueTask DownloadOrOpenPage(NexusModsFileMetadata.ReadOnly fileMetadata, CancellationToken cancellationToken)
    {
        if (_loginManager.IsPremium)
        {
            await using var tempPath = _temporaryFileManager.CreateFile();
            var job = await _nexusModsLibrary.CreateDownloadJob(tempPath, fileMetadata.Uid.GameId, fileMetadata.ModPage.Uid.ModId, fileMetadata.Uid.FileId, cancellationToken: cancellationToken);
            await _libraryService.AddDownload(job);
        }
        else
        {
            await _osInterop.OpenUrl(fileMetadata.GetUri(), logOutput: false, fireAndForget: true, cancellationToken: cancellationToken);
        }
    }

    public string Name => _collection.Name;
    public string Summary => _collection.Summary;
    public int ModCount => _revision.Downloads.Count;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong DownloadCount => (ulong)_revision.Downloads.Count;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public string AuthorName => _collection.Author.Name;

    public CollectionSlug Slug => _collection.Slug;
    public RevisionNumber RevisionNumber => _revision.RevisionNumber;
    public int RequiredModCount { get; }
    public int OptionalModCount { get; }

    [Reactive] public Bitmap? TileImage { get; private set; }
    [Reactive] public Bitmap? BackgroundImage { get; private set; }
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
