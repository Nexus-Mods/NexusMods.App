using System.Collections.ObjectModel;
using System.ComponentModel;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Kernel;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc />
public class DownloadService : IDownloadService, IDisposable, IHostedService
{
    /// <inheritdoc />
    public ReadOnlyObservableCollection<IDownloadTask> Downloads => _downloadsCollection;
    
    /// <inheritdoc />
    public AbsolutePath OngoingDownloadsDirectory => _downloadDirectory;
    private ReadOnlyObservableCollection<IDownloadTask> _downloadsCollection = ReadOnlyObservableCollection<IDownloadTask>.Empty;

    private readonly SourceCache<IDownloadTask, EntityId> _downloads = new(t => t.PersistentState.Id);
    private readonly ILogger<DownloadService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IConnection _conn;
    private bool _isDisposed;
    private readonly CompositeDisposable _disposables;
    private readonly IFileStore _fileStore;
    private AbsolutePath _downloadDirectory;

    public DownloadService(
        ILogger<DownloadService> logger, 
        IServiceProvider provider, 
        IFileStore fileStore, 
        IConnection conn,
        IFileSystem fs,
        ISettingsManager settingsManager)
    {
        _logger = logger;
        _provider = provider;
        _conn = conn;
        _disposables = new CompositeDisposable();
        _fileStore = fileStore;
        _downloadDirectory = settingsManager.Get<DownloadSettings>().OngoingDownloadLocation.ToPath(fs);
        if (!_downloadDirectory.DirectoryExists())
        {
            _downloadDirectory.CreateDirectory();
        }
    }

    internal IEnumerable<IDownloadTask> GetItemsToResume()
    {
        var db = _conn.Db;

        var tasks = db.Find(DownloaderState.Status)
            .Select(x => DownloaderState.Load(db, x))
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        
        _disposables.Dispose();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _conn.Revisions
            // This is really inefficient, but we'd need to rewrite other parts of this service
            // to process these updates in a more efficient way, so we'll come back to this later.
            // We should be using ObserveDatoms here
            .SelectMany(revision =>
            {
                return revision.AddedDatoms
                    .Select(r => r.Resolved)
                    .Where(d => d.A == DownloaderState.Status)
                    .Select(d => (revision.Database, d.E));
            })
            .StartWith(DownloaderState.All(_conn.Db).Select(state => (state.Db, state.Id)))
            .Subscribe(x =>
            {
                var (db, id) = x;
                _downloads.Edit(e =>
                {
                    var found = e.Lookup(id);
                    if (found.HasValue) 
                        found.Value.RefreshState();
                    else
                    {
                        var task = GetTaskFromState(DownloaderState.Load(db, id));
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
        
        var db = _conn.Db;
        // Cancel any orphaned downloads
        foreach (var task in  DownloaderState.FindByStatus(db, DownloadTaskStatus.Downloading))
        { 
            try
            {
                _logger.LogInformation("Cancelling orphaned download task {Task}", task);
                var downloadTask = GetTaskFromState(task);
                downloadTask?.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While cancelling orphaned download task {Task}", task);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var suspendingTasks = _downloads.Items
            .Where(dl => dl.PersistentState.Status == DownloadTaskStatus.Downloading)
            .Select(dl => dl.Suspend());
        
        await Task.WhenAll(suspendingTasks);
    }
    
    /// <summary>
    /// Set a custom downloadDirectory, for tests only.
    /// Directory should already exist.
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal void SetDownloadDirectory(AbsolutePath path)
    {
        _downloadDirectory = path;
    }
}
