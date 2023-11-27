using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Types;
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
    private readonly IDataStore _store;
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

    public DownloadService(ILogger<DownloadService> logger, IServiceProvider provider, IDataStore store, IFileOriginRegistry fileOriginRegistry)
    {
        _logger = logger;
        _provider = provider;
        _store = store;
        _fileOriginRegistry = fileOriginRegistry;

        _tasks = new SourceList<IDownloadTask>();
        _tasksChangeSet = _tasks.Connect();
        _tasksChangeSet.Bind(out _currentDownloads);
        _tasks.AddRange(GetItemsToResume());
    }

    internal IEnumerable<IDownloadTask> GetItemsToResume()
    {
        return _store.AllIds(EntityCategory.DownloadStates)
            .Select(id => _store.Get<DownloaderState>(id))
            .Where(x => x!.Status != DownloadTaskStatus.Completed)
            .Select(state => GetTaskFromState(state!))
            .Where(x => x != null)
            .Cast<IDownloadTask>();
    }

    internal IDownloadTask? GetTaskFromState(DownloaderState state)
    {
        if (state.Status == DownloadTaskStatus.Completed)
            return null;

        switch (state.TypeSpecificData)
        {
            case HttpDownloadState:
            {
                var task = new HttpDownloadTask(_provider.GetRequiredService<ILogger<HttpDownloadTask>>(), _provider.GetRequiredService<TemporaryFileManager>(), _provider.GetRequiredService<HttpClient>(), _provider.GetRequiredService<IHttpDownloader>(), this);
                task.RestoreFromSuspend(state);
                return task;
            }
            case NxmDownloadState:
            {
                var task = new NxmDownloadTask(_provider.GetRequiredService<TemporaryFileManager>(), _provider.GetRequiredService<Client>(), _provider.GetRequiredService<IHttpDownloader>(), this);
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
    public Task AddNxmTask(NXMUrl url)
    {
        var task = _provider.GetRequiredService<NxmDownloadTask>();
        task.Init(url);
        return AddTask(task);
    }

    /// <inheritdoc />
    public Task AddHttpTask(string url)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        task.Init(url);
        return AddTask(task);
    }

    /// <inheritdoc />
    public Task AddTask(IDownloadTask task)
    {
        AddTaskWithoutStarting(task);
        var item = task.StartAsync();
        return item;
    }

    // For test use, too.
    internal void AddTaskWithoutStarting(IDownloadTask task)
    {
        _tasks.Add(task);
        _started.OnNext(task);
        PersistOnStart(task);
    }

    /// <inheritdoc />
    public void OnComplete(IDownloadTask task)
    {
        UpdateInDatastore(task);
        _tasks.Remove(task);
        _completed.OnNext(task);
    }

    /// <inheritdoc />
    public void OnCancelled(IDownloadTask task)
    {
        DeleteFromDatastore(task);
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
        var provider = DateTimeProvider.Instance;
        var totalThroughput = 0L;
        foreach (var download in _currentDownloads)
            totalThroughput += download.CalculateThroughput(provider);

        return Size.FromLong(totalThroughput);
    }

    /// <inheritdoc />
    public void UpdatePersistedState(IDownloadTask task) => UpdateInDatastore(task);

    /// <inheritdoc />
    public async Task FinalizeDownloadAsync(IDownloadTask task, TemporaryPath path, string modName)
    {
        task.Status = DownloadTaskStatus.Installing;

        try
        {
            // TODO: Fix this
            var downloadId = await _fileOriginRegistry.RegisterDownload(path.Path, new FilePathMetadata
            {
                Name = modName,
                OriginalName = path.Path.FileName,
                Quality = Quality.Low
            });
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

    private void PersistOnStart(IDownloadTask task)
    {
        var state = task.ExportState();
        _store.Put(new IdVariableLength(EntityCategory.DownloadStates, state.DownloadPath), state);
    }

    private void DeleteFromDatastore(IDownloadTask task)
    {
        _store.Delete(new IdVariableLength(EntityCategory.DownloadStates, task.ExportState().DownloadPath));
    }

    private void UpdateInDatastore(IDownloadTask task)
    {
        // Note: It's easier to re-generate state rather than updating existing instance.
        var state = task.ExportState();
        _store.Delete(new IdVariableLength(EntityCategory.DownloadStates, state.DownloadPath));
        _store.Put(new IdVariableLength(EntityCategory.DownloadStates, state.DownloadPath), state);
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
