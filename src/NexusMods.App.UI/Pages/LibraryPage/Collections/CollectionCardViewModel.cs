using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI;
using System.Reactive;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public class CollectionCardViewModel : AViewModel<ICollectionCardViewModel>, ICollectionCardViewModel
{
    private readonly CollectionRevisionMetadata.ReadOnly _revision;
    private readonly CollectionMetadata.ReadOnly _collection;

    public CollectionCardViewModel(IWindowManager windowManager, IConnection connection, RevisionId revision, LoadoutId loadoutId)
    {
        _revision = CollectionRevisionMetadata.FindByRevisionId(connection.Db, revision)
            .First();
        _collection = _revision.Collection;
        
        ShowDetailsCommand = ReactiveCommand.Create<NavigationInformation, Unit>(info =>
        {
            var pageData = new PageData
            {
                FactoryId = CollectionDownloadPageFactory.StaticId,
                Context = new CollectionDownloadPageContext
                {
                    RevisionId = revision,
                    LoadoutId = loadoutId,
                },
            };

            var workspaceController = windowManager.ActiveWorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            var workspaceId = workspaceController.ActiveWorkspaceId;
            workspaceController.OpenPage(workspaceId, pageData, behavior);
            return Unit.Default;
        });
    }

    public string Name => _collection.Name;
    public Bitmap Image => new(new MemoryStream(_collection.TileImage.ToArray()));
    public string Summary => _collection.Summary;
    public string Category => string.Join(" \u2022 ", _collection.Tags.Select(t => t.Name));
    public int ModCount => _revision.Files.Count;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong DownloadCount => _revision.Downloads;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public string AuthorName => _collection.Author.Name;
    public Bitmap AuthorAvatar => new(new MemoryStream(_collection.Author.AvatarImage.ToArray()));
    public ReactiveCommand<NavigationInformation, Unit> ShowDetailsCommand { get; }
}
