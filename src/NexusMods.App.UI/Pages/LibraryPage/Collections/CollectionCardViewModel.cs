using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Resources;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
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
        IResourceLoader<EntityId, Bitmap> tileImagePipeline,
        IWindowManager windowManager,
        WorkspaceId workspaceId,
        IConnection connection,
        RevisionId revision)
    {
        _revision = CollectionRevisionMetadata.FindByRevisionId(connection.Db, revision).First();
        _collection = _revision.Collection;

        var workspaceController = windowManager.ActiveWorkspaceController;

        OpenCollectionDownloadPageCommand = new ReactiveCommand<NavigationInformation>(execute: info =>
        {
            var page = new PageData
            {
                Context = new CollectionDownloadPageContext
                {
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
        });
    }

    public string Name => _collection.Name;
    [Reactive] public Bitmap? Image { get; private set; }
    public string Summary => _collection.Summary;
    public string Category => _revision.AdultContent ? "Adult" : "Non-Adult";
    public int ModCount => _revision.Files.Count;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong DownloadCount => _revision.Downloads;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public string AuthorName => _collection.Author.Name;
    public Bitmap AuthorAvatar => new(new MemoryStream(_collection.Author.AvatarImage.ToArray()));
    public ReactiveCommand<NavigationInformation> OpenCollectionDownloadPageCommand { get; }
}
