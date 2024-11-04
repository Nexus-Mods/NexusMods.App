using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Resources;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
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
    public CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; }

    public CollectionDownloadViewModel(
        IWindowManager windowManager,
        NexusModsDataProvider nexusModsDataProvider,
        IResourceLoader<EntityId, Bitmap> tileImagePipeline,
        IResourceLoader<EntityId, Bitmap> backgroundImagePipeline,
        CollectionRevisionMetadata.ReadOnly revisionMetadata) : base(windowManager)
    {
        _revision = revisionMetadata;
        _collection = revisionMetadata.Collection;
        TreeDataGridAdapter = new CollectionDownloadTreeDataGridAdapter(nexusModsDataProvider, revisionMetadata);
        TreeDataGridAdapter.ViewHierarchical.Value = false;

        // TODO:
        CollectionStatusText = "TODO";

        var requiredModCount = 0;
        var optionalModCount = 0;
        foreach (var file in _revision.Files)
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

            nexusModsDataProvider
                .ObserveCollectionItems(revisionMetadata)
                .SubscribeWithErrorLogging()
                .AddTo(disposables);
        });
    }

    public string Name => _collection.Name;
    public string Summary => _collection.Summary;
    public int ModCount => _revision.Files.Count;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong DownloadCount => _revision.Downloads;
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

public class CollectionDownloadTreeDataGridAdapter : TreeDataGridAdapter<ILibraryItemModel, EntityId>
{
    private readonly NexusModsDataProvider _nexusModsDataProvider;
    private readonly CollectionRevisionMetadata.ReadOnly _revisionMetadata;

    public CollectionDownloadTreeDataGridAdapter(NexusModsDataProvider nexusModsDataProvider, CollectionRevisionMetadata.ReadOnly revisionMetadata)
    {
        _nexusModsDataProvider = nexusModsDataProvider;
        _revisionMetadata = revisionMetadata;
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
}
