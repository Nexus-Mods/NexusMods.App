using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Resources;
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
        IConnection connection,
        RevisionId revision)
    {
        _revision = CollectionRevisionMetadata.FindByRevisionId(connection.Db, revision).First();
        _collection = _revision.Collection;

        this.WhenActivated(disposables =>
        {
            Observable
                .Return(_collection.Id)
                .ObserveOnThreadPool()
                .SelectAwait(async (id, cancellationToken) => await tileImagePipeline.LoadResourceAsync(id, cancellationToken), configureAwait: false)
                .Select(static resource => resource.Data)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.Image = bitmap)
                .AddTo(disposables);
        });
    }

    public string Name => _collection.Name;
    [Reactive] public Bitmap? Image { get; private set; }
    public string Summary => _collection.Summary;
    public string Category => string.Join(" \u2022 ", _collection.Tags.Select(t => t.Name));
    public int ModCount => _revision.Files.Count;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong DownloadCount => _revision.Downloads;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public string AuthorName => _collection.Author.Name;
    public Bitmap AuthorAvatar => new(new MemoryStream(_collection.Author.AvatarImage.ToArray()));
}
