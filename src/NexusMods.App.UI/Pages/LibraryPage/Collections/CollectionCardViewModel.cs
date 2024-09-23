using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public class CollectionCardViewModel : AViewModel<ICollectionCardViewModel>, ICollectionCardViewModel
{
    private readonly CollectionRevision.ReadOnly _revision;
    private readonly Collection.ReadOnly _collection;

    public CollectionCardViewModel(IConnection connection, RevisionId revision)
    {
        _revision = CollectionRevision.FindByRevisionId(connection.Db, revision)
            .First();
        _collection = _revision.Collection;
    }

    public string Name => _collection.Name;
    public Bitmap Image => new(new MemoryStream(_collection.TileImage.ToArray()));
    public string Summary => _collection.Summary;
    public string Category => string.Join(" \u2022 ", _collection.Tags.Select(t => t.Name));
    public int ModCount => (int)_revision.ModCount;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong DownloadCount => _revision.Downloads;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public string AuthorName => _collection.User.Name;
    public Bitmap AuthorAvatar => new(new MemoryStream(_collection.User.AvatarImage.ToArray()));
}
