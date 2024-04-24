using System.Collections.ObjectModel;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using System.Reactive.Disposables;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc />
public class DownloadService : IDownloadService, IAsyncDisposable
{
    /// <inheritdoc />
    public ReadOnlyObservableCollection<IDownloadTask> Downloads => _downloadsCollection;
    private readonly ReadOnlyObservableCollection<IDownloadTask> _downloadsCollection;

    private readonly SourceCache<IDownloadTask, EntityId> _downloads = new(t => t.PersistentState.Id);
    private readonly ILogger<DownloadService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IConnection _conn;
    private bool _isDisposed;
    private readonly CompositeDisposable _disposables;

    public DownloadService(ILogger<DownloadService> logger, IServiceProvider provider, IConnection conn)
    {
        _logger = logger;
        _provider = provider;
        _conn = conn;
        _disposables = new CompositeDisposable();

        _conn.UpdatesFor(DownloaderState.Status)
            .Subscribe(x =>
            {
                var (db, id) = x;
                _downloads.Edit(e =>
                {
                    var found = e.Lookup(id);
                    if (found.HasValue) 
                        found.Value.ResetState(db);
                    else
                    {
                        var task = GetTaskFromState(db.Get<DownloaderState.Model>(id));
                        if (task == null)
                            return;
                        e.AddOrUpdate(task);
                    }
                });
            })
            .DisposeWith(_disposables);

        _downloads.Connect()
            .Bind(out _downloadsCollection)
            .Subscribe()
            .DisposeWith(_disposables);
    }

    internal IEnumerable<IDownloadTask> GetItemsToResume()
    {
        var db = _conn.Db;

        var tasks = db.Find(DownloaderState.Status)
            .Select(x => db.Get<DownloaderState.Model>(x))
            .Where(x => x.Status != DownloadTaskStatus.Completed && 
                             x.Status != DownloadTaskStatus.Cancelled)
            .Select(GetTaskFromState)
            .Where(x => x != null)
            .Cast<IDownloadTask>();
        return tasks;
    }

    internal IDownloadTask? GetTaskFromState(DownloaderState.Model state)
    {
        var httpState = _provider.GetRequiredService<HttpDownloadTask>();
        httpState.Init(state);
        return httpState;
    }
    
    /// <inheritdoc />
    public async Task<IDownloadTask> AddTask(NXMUrl url)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<IDownloadTask> AddTask(Uri url)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        await task.Create(url);
        return _downloads.Lookup(task.PersistentState.Id).Value;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;
        
        _disposables.Dispose();
        
        foreach (var download in _downloads.Items)
        {
            if (download.PersistentState.Status == DownloadTaskStatus.Downloading)
                await download.Cancel();
        }
        _isDisposed = true;
    }
}
