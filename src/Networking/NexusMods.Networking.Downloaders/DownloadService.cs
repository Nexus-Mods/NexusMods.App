using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc />
public class DownloadService : IDownloadService
{
    /// <inheritdoc />
    public IObservable<IChangeSet<IDownloadTask>> Downloads => _tasksChangeSet;

    private readonly SourceList<IDownloadTask> _tasks;
    private readonly ILogger<DownloadService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IConnection _conn;
    private readonly Subject<IDownloadTask> _started = new();
    private readonly Subject<IDownloadTask> _completed = new();
    private readonly Subject<IDownloadTask> _cancelled = new();
    private readonly Subject<IDownloadTask> _paused = new();
    private readonly Subject<IDownloadTask> _resumed = new();
    private readonly Subject<(IDownloadTask task, DownloadId analyzedHash, string modName)> _analyzed = new();
    private readonly IObservable<IChangeSet<IDownloadTask>> _tasksChangeSet;
    private readonly ReadOnlyObservableCollection<IDownloadTask> _currentDownloads;
    private bool _isDisposed = false;
    private readonly IFileOriginRegistry _fileOriginRegistry;

    public DownloadService(ILogger<DownloadService> logger, IServiceProvider provider, 
        IConnection conn, IFileOriginRegistry fileOriginRegistry)
    {
        _logger = logger;
        _provider = provider;
        _conn = conn;
        _fileOriginRegistry = fileOriginRegistry;

        _tasks = new SourceList<IDownloadTask>();
        _tasksChangeSet = _tasks.Connect();
        _tasksChangeSet.Bind(out _currentDownloads).Subscribe();
        _tasks.AddRange(GetItemsToResume());
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
        if (state.Status == DownloadTaskStatus.Completed)
            return null;
        
        // Datomic, XTDB, Datahike

        if (state.Contains(NxmDownloadState.Query))
        {
            // Load from NxmDownloadState
        }
        else if (state.Contains(HttpDownloadState.Query))
        {
            var task = new HttpDownloadTask(_provider.GetRequiredService<ILogger<HttpDownloadTask>>(), 
                _provider.GetRequiredService<TemporaryFileManager>(), 
                _provider.GetRequiredService<HttpClient>(), 
                _provider.GetRequiredService<IHttpDownloader>(), this);
            task.RestoreFromSuspend(state);
            return task;
        }
        else
        {
            _logger.LogError("Unrecognised Type Specific Data. {StateTypeSpecificData}", state.TypeSpecificData);
            return null;
        }

        
        switch (state)
        {
            case { NexusModsDownloadState : { 42 }}:
            {
                var task = new NexusModsDownloadTask(_provider.GetRequiredService<TemporaryFileManager>(), 
                    _provider.GetRequiredService<INexusApiClient>(), 
                    _provider.GetRequiredService<IHttpDownloader>(), this);
                task.RestoreFromSuspend(state);
                return task;
            }
            
            case HttpDownloadState:
            {

            }
            case NxmDownloadState:
            {
                var task = new NxmDownloadTask(_provider.GetRequiredService<TemporaryFileManager>(), _provider.GetRequiredService<INexusApiClient>(), _provider.GetRequiredService<IHttpDownloader>(), this);
                task.RestoreFromSuspend(state);
                return task;
            }
            default:
            {
                _logger.LogError("Unrecognised Type Specific Data. {StateTypeSpecificData}", state.TypeSpecificData);
                return null;
            }
        }
    }

    /// <inheritdoc />
    public IObservable<IDownloadTask> StartedTasks => _started;

    /// <inheritdoc />
    public IObservable<IDownloadTask> CompletedTasks => _completed;

    /// <inheritdoc />
    public IObservable<IDownloadTask> CancelledTasks => _cancelled;

    /// <inheritdoc />
    public IObservable<IDownloadTask> PausedTasks => _paused;

    /// <inheritdoc />
    public IObservable<IDownloadTask> ResumedTasks => _resumed;

    /// <inheritdoc />
    public IObservable<(IDownloadTask task, DownloadId downloadId, string modName)> AnalyzedArchives => _analyzed;

    /// <inheritdoc />
    public async Task AddNxmTask(NXMUrl url)
    {
        var task = _provider.GetRequiredService<NxmDownloadTask>();
        await task.Init(url);
        return AddTask(task);
    }

    /// <inheritdoc />
    public Task AddHttpTask(string url)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        await task.Init(url);
        return AddTask(task);
    }

    /// <inheritdoc />
    public async Task AddTask(IDownloadTask task)
    {
        await AddTaskWithoutStarting(task);
        var item = await task.StartAsync();
        return item;
    }

    // For test use, too.
    internal async Task AddTaskWithoutStarting(IDownloadTask task)
    {
        _tasks.Add(task);
        _started.OnNext(task);
    }

    /// <inheritdoc />
    public async Task OnComplete(IDownloadTask task)
    {
        await UpdateInDatastore(task);
        _tasks.Remove(task);
        _completed.OnNext(task);
    }

    /// <inheritdoc />
    public async Task OnCancelled(IDownloadTask task)
    {
        task.Status = DownloadTaskStatus.Cancelled;
        await UpdateInDatastore(task);
        _tasks.Remove(task);
        _cancelled.OnNext(task);
    }

    /// <inheritdoc />
    public void OnPaused(IDownloadTask task)
    {
        _paused.OnNext(task);
        UpdatePersistedState(task);
    }

    /// <inheritdoc />
    public void OnResumed(IDownloadTask task) => _resumed.OnNext(task);

    /// <inheritdoc />
    public Size GetThroughput()
    {
        var totalThroughput = 0L;
        foreach (var download in _currentDownloads)
            totalThroughput += download.CalculateThroughput();

        return Size.FromLong(totalThroughput);
    }

    /// <inheritdoc />
    public Optional<Percent> GetTotalProgress()
    {
        var totalDownloadedBytes = Size.Zero;
        var totalSizeBytes = Size.Zero;
        var active = false;

        foreach (var dl in _currentDownloads.Where(x => x.Status == DownloadTaskStatus.Downloading))
        {
            // Only compute percent for downloads that have a known size
            if (dl is not IHaveFileSize size) continue;

            totalSizeBytes += size.SizeBytes;
            totalDownloadedBytes += dl.DownloadedSizeBytes;
            active = true;
        }

        if (!active || totalSizeBytes == 0 || totalSizeBytes <= totalDownloadedBytes)
        {
            return Optional.None<Percent>();
        }

        return new Percent(totalDownloadedBytes.Value / (double) totalSizeBytes);
    }

    /// <inheritdoc />
    public Task UpdatePersistedState(IDownloadTask task) => UpdateInDatastore(task);

    /// <inheritdoc />
    public async Task FinalizeDownloadAsync(IDownloadTask task, TemporaryPath path, string modName)
    {
        task.Status = DownloadTaskStatus.Installing;

        try
        {
            // TODO: Fix this so we properly log NexusMods info with Nexus metadata.
            var downloadId = await _fileOriginRegistry.RegisterDownload(path.Path);
            _analyzed.OnNext((task, downloadId, modName));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to analyze archive, {Message}\n{Stacktrace}", e.Message, e.StackTrace);
            throw;
        }
        finally
        {
            // Make sure we don't leave anything dangling on disk!
            await path.DisposeAsync();

            // Maybe add a failed state; we don't have UI designs for this however.
            task.Status = DownloadTaskStatus.Completed;
            OnComplete(task);
        }
    }
    
    private async Task UpdateInDatastore(IDownloadTask task)
    {

    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        // Pause all tasks, which persists them to datastore.
        foreach (var task in _currentDownloads)
            task.Suspend();

        _isDisposed = true;
        _tasks.Dispose();
        _started.Dispose();
        _completed.Dispose();
        _cancelled.Dispose();
        _paused.Dispose();
        _resumed.Dispose();
        _analyzed.Dispose();
    }
}
