using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    private readonly IConnection _connection;
    private readonly CollectionDownloadPageContext _context;
    private readonly CollectionRevisionMetadata.ReadOnly _revision;

    public CollectionDownloadViewModel(IWindowManager manager, IConnection connection, CollectionDownloadPageContext context) : base(manager)
    {
        _connection = connection;
        _context = context;
        
        _revision = CollectionRevisionMetadata.FindByRevisionId(connection.Db, context.RevisionId)
            .First();
        
    }

    public string Name => _revision.Collection.Name;
    public CollectionSlug Slug => _revision.Collection.Slug;
    public RevisionNumber RevisionNumber => _revision.RevisionNumber;
    public string AuthorName => _revision.Collection.Author.Name;
    public string Summary => _revision.Collection.Summary;
    public int ModCount => _revision.Files.Count;
    public int RequiredModCount => _revision.Files.Count(f => f.IsOptional == false);
    public int OptionalModCount => _revision.Files.Count(f => f.IsOptional);
    public int EndorsementCount => (int)_revision.Collection.Endorsements;
    public int DownloadCount => (int)_revision.Downloads;
    public Size TotalSize => _revision.TotalSize;
    public Percent OverallRating => Percent.CreateClamped(_revision.OverallRating);
    public Bitmap TileImage => new(new MemoryStream(_revision.Collection.TileImage.ToArray()));
    public Bitmap BackgroundImage => new(new MemoryStream(_revision.Collection.BackgroundImage.ToArray()));
    public string CollectionStatusText => $"0 of {ModCount} mods downloaded";
}
