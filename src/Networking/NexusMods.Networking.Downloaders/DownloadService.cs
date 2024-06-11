using System.Collections.ObjectModel;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Kernel;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.IO;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;

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
    private readonly IFileStore _fileStore;

    public DownloadService(ILogger<DownloadService> logger, IServiceProvider provider, IFileStore fileStore, IConnection conn)
    {
        _logger = logger;
        _provider = provider;
        _conn = conn;
        _disposables = new CompositeDisposable();
        _fileStore = fileStore;
        
        _conn.ObserveDatoms(SliceDescriptor.Create(DownloaderState.Status, _conn.Registry))
            .QueryWhenChanged(datoms => datoms.Select(d => d.E))
            .Subscribe(ids =>
            {
                var db = _conn.Db;
                _downloads.Edit(e =>
                {
                    foreach (var id in ids)
                    {
                        var found = e.Lookup(id);
                        if (found.HasValue)
                            found.Value.ResetState(db);
                        else
                        {
                            var task = GetTaskFromState(DownloaderState.Load(db, id));
                            if (task == null)
                                return;
                            e.AddOrUpdate(task);
                        }
                    }
                });
            })
            .DisposeWith(_disposables);

        _downloads.Connect()
            .Bind(out _downloadsCollection)
            .Subscribe()
            .DisposeWith(_disposables);
        
        // Cancel any orphaned downloads
        foreach (var state in DownloaderState.FindByStatus(_conn.Db, DownloadTaskStatus.Downloading))
        {
            try
            {
                _logger.LogInformation("Cancelling orphaned download task {Task}", state.FriendlyName);
                var downloadTask = GetTaskFromState(state);
                downloadTask?.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While cancelling orphaned download task {Task}", state.FriendlyName);
            }
        }
    }

    internal IEnumerable<IDownloadTask> GetItemsToResume()
    {
        var db = _conn.Db;

        var tasks = DownloaderState.All(db)
            .Where(x => x.Status != DownloadTaskStatus.Completed && 
                             x.Status != DownloadTaskStatus.Cancelled)
            .Select(GetTaskFromState)
            .Where(x => x != null)
            .Cast<IDownloadTask>();
        return tasks;
    }

    internal IDownloadTask? GetTaskFromState(DownloaderState.ReadOnly state)
    {
        if (state.Contains(HttpDownloadState.Uri))
        {
            var httpState = _provider.GetRequiredService<HttpDownloadTask>();
            httpState.Init(state);
            return httpState;
        }
        else if (state.Contains(NxmDownloadState.ModId))
        {
            var nxmState = _provider.GetRequiredService<NxmDownloadTask>();
            nxmState.Init(state);
            return nxmState;
        }
        else
        {
            throw new InvalidOperationException("Unknown download task type: " + state);
        }
    }
    
    /// <inheritdoc />
    public async Task<IDownloadTask> AddTask(NXMModUrl url)
    {
        _logger.LogInformation("Adding task for {Url}", url);
        var task = _provider.GetRequiredService<NxmDownloadTask>();
        await task.Create(url);
        return _downloads.Lookup(task.PersistentState.Id).Value;
    }

    /// <inheritdoc />
    public async Task<IDownloadTask> AddTask(Uri url)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        await task.Create(url);
        return _downloads.Lookup(task.PersistentState.Id).Value;
    }

    /// <inheritdoc />
    public Size GetThroughput()
    {
        var tasks = _downloads.Items
            .Where(i => i.PersistentState.Status == DownloadTaskStatus.Downloading)
            .ToArray();
        
        return tasks.Length == 0 
            ? Size.Zero 
            : tasks.Aggregate(Size.Zero, (acc, x) => acc + Size.From(x.Bandwidth.Value));
    }

    /// <inheritdoc />
    public Optional<Percent> GetTotalProgress()
    {
        var tasks = _downloads.Items
            .Where(i => i.PersistentState.Status == DownloadTaskStatus.Downloading)
            .ToArray();
        
        if (tasks.Length == 0)
            return Optional<Percent>.None;
        
        var total = tasks
            .Aggregate(0.0, (acc, x) => acc + x.Progress.Value);
        return Optional<Percent>.Create(Percent.CreateClamped(total / tasks.Length));
    }

    /// <inheritdoc />
    public async Task SetIsHidden(bool isHidden, IDownloadTask[] targets)
    {
        using var tx = _conn.BeginTransaction();
        foreach (var downloadTask in targets)
        {
            downloadTask.SetIsHidden(isHidden, tx);
        }
        await tx.Commit();
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
