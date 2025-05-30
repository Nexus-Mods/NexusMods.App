using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Sdk.Resources;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public class CollectionCardViewModel : AViewModel<ICollectionCardViewModel>, ICollectionCardViewModel
{
    private readonly CollectionRevisionMetadata.ReadOnly _revision;
    private readonly CollectionMetadata.ReadOnly _collection;

    public CollectionCardViewModel(
        CollectionDownloader collectionDownloader,
        IResourceLoader<EntityId, Bitmap> tileImagePipeline,
        IResourceLoader<EntityId, Bitmap> userAvatarPipeline,
        IWindowManager windowManager,
        WorkspaceId workspaceId,
        CollectionRevisionMetadata.ReadOnly revision,
        LoadoutId targetLoadout)
    {
        _revision = revision;
        _collection = _revision.Collection;

        var workspaceController = windowManager.ActiveWorkspaceController;

        OpenCollectionDownloadPageCommand = new ReactiveCommand<NavigationInformation>(execute: info =>
        {
            var page = new PageData
            {
                Context = new CollectionDownloadPageContext
                {
                    TargetLoadout = targetLoadout,
                    CollectionRevisionMetadataId = _revision,
                },
                FactoryId = CollectionDownloadPageFactory.StaticId,
            };

            var behavior = workspaceController.GetOpenPageBehavior(page, info);
            workspaceController.OpenPage(workspaceId, page, behavior);
        });

        this.WhenActivated(disposables =>
        {
            ImagePipelines.CreateObservable(_collection.Id, tileImagePipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.Image = bitmap)
                .AddTo(disposables);

            ImagePipelines.CreateObservable(_collection.Author.Id, userAvatarPipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.AuthorAvatar = bitmap)
                .AddTo(disposables);

            var collectionGroupObservable = collectionDownloader.GetCollectionGroupObservable(_revision, targetLoadout);
            collectionDownloader
                .IsCollectionInstalledObservable(_revision, collectionGroupObservable)
                .OnUI()
                .SubscribeWithErrorLogging(isInstalled => IsCollectionInstalled = isInstalled)
                .AddTo(disposables);
        });
    }

    public string Name => _collection.Name;
    [Reactive] public Bitmap? Image { get; private set; }
    [Reactive] public Bitmap? AuthorAvatar { get; private set; }
    public string Summary => _collection.Summary.ValueOr(string.Empty);
    public string Category => _collection.Category.Name;
    public int NumDownloads => _revision.Downloads.Count;
    public RevisionNumber RevisionNumber => _revision.RevisionNumber;
    public ulong EndorsementCount => _collection.Endorsements.ValueOr(0ul);
    public ulong TotalDownloads => _collection.TotalDownloads.ValueOr(0ul);
    public Size TotalSize => _revision.TotalSize.ValueOr(Size.Zero);
    public Percent OverallRating => Percent.Create(_revision.Collection.RecentRating.ValueOr(0), maximum: 100);

    public bool IsAdult => _revision.IsAdult.ValueOr(false);
    public string AuthorName => _collection.Author.Name;
    public ReactiveCommand<NavigationInformation> OpenCollectionDownloadPageCommand { get; }

    [Reactive] public bool IsCollectionInstalled { get; private set; }
}
