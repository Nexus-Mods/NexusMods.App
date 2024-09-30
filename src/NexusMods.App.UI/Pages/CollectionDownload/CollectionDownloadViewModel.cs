using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    private readonly IConnection _connection;
    private readonly CollectionDownloadPageContext _context;
    private readonly CollectionRevisionMetadata.ReadOnly _revision;
    private readonly Loadout.ReadOnly _loadout;

    public CollectionDownloadViewModel(IWindowManager manager, IServiceProvider serviceProvider, CollectionDownloadPageContext context) : base(manager)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _context = context;
        
        var db = _connection.Db;
        _revision = CollectionRevisionMetadata.FindByRevisionId(db, context.RevisionId)
            .First();
        
        _loadout = Loadout.Load(db, context.LoadoutId);
        
        var ticker = R3.Observable
            .Interval(period: TimeSpan.FromSeconds(30), timeProvider: ObservableSystem.DefaultTimeProvider)
            .ObserveOnUIThreadDispatcher()
            .Select(_ => DateTime.Now)
            .Publish(initialValue: DateTime.Now);

        var filter = new LibraryFilter(System.Reactive.Linq.Observable.Return(context.LoadoutId), 
            System.Reactive.Linq.Observable.Return(_loadout.InstallationInstance.Game));
        
        RequiredModsAdapter = new LibraryTreeDataGridAdapter(serviceProvider, ticker, filter);
        OptionalModsAdapter = new LibraryTreeDataGridAdapter(serviceProvider, ticker, filter);
        
        this.WhenActivated(d =>
        {
            RequiredModsAdapter.Activate();
            Disposable.Create(RequiredModsAdapter, static adapter => adapter.Deactivate()).AddTo(d);

            OptionalModsAdapter.Activate();
            Disposable.Create(OptionalModsAdapter, static adapter => adapter.Deactivate()).AddTo(d);
        });
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
    public LibraryTreeDataGridAdapter RequiredModsAdapter { get; }
    public LibraryTreeDataGridAdapter OptionalModsAdapter { get; }
}
