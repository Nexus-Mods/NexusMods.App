using Avalonia.Media.Imaging;
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
    public Bitmap Image => throw new NotImplementedException();
    public string Summary => _collection.Summary;
    public string Category => "TODO";
    public int ModCount => -1;
    public ulong EndorsementCount => _collection.Endorsements;
    public ulong DownloadCount => _revision.Downloads;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public string AuthorName => _collection.User.Name;
    public Bitmap AuthorAvatar => throw new NotImplementedException();
}
