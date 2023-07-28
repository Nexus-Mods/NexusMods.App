using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc />
public class DownloadService : IDownloadService
{
    /// <inheritdoc />
    public ReadOnlyObservableCollection<IDownloadTask> Downloads => _downloads;

    private readonly SourceList<IDownloadTask> _tasks;
    private ReadOnlyObservableCollection<IDownloadTask> _downloads;
    private readonly ILogger<DownloadService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IDataStore _store;
    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly Subject<IDownloadTask> _started = new();
    private readonly Subject<IDownloadTask> _completed = new();
    private readonly Subject<IDownloadTask> _cancelled = new();
    private readonly Subject<IDownloadTask> _paused = new();
    private readonly Subject<IDownloadTask> _resumed = new();
    private readonly Subject<(IDownloadTask task, Hash analyzedHash, string modName)> _analyzed = new();

    public DownloadService(ILogger<DownloadService> logger, IServiceProvider provider, IDataStore store, IArchiveAnalyzer archiveAnalyzer)
    {
        _logger = logger;
        _provider = provider;
        _store = store;
        _archiveAnalyzer = archiveAnalyzer;

        _tasks = new SourceList<IDownloadTask>();
        _tasks.Connect()
              .Bind(out _downloads);

        _tasks.AddRange(GetItemsToResume());
    }

    internal IEnumerable<IDownloadTask> GetItemsToResume()
    {
        return _store.AllIds(EntityCategory.DownloadStates)
            .Select(id => _store.Get<DownloaderState>(id))
            .Where(x => x!.Status != DownloadTaskStatus.Completed)
            .Select(state => GetTaskFromState(state!));
    }

    internal IDownloadTask GetTaskFromState(DownloaderState state)
    {
        switch (state.TypeSpecificData)
        {
            case HttpDownloadState:
            {
                var task = _provider.GetRequiredService<HttpDownloadTask>();
                task.RestoreFromSuspend(state);
                return task;
            }
            case NxmDownloadState:
            {
                var task = _provider.GetRequiredService<NxmDownloadTask>();
                task.RestoreFromSuspend(state);
                return task;
            }
            default:
                throw new Exception("Unrecognised Type Specific Data.");
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
    public IObservable<(IDownloadTask task, Hash analyzedHash, string modName)> AnalyzedArchives => _analyzed;

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
        UpdateAsComplete(task);
        _tasks.Remove(task);
        _completed.OnNext(task);
    }

    /// <inheritdoc />
    public void OnCancelled(IDownloadTask task)
    {
        DeleteFromDatastore(task);
        _cancelled.OnNext(task);
    }

    /// <inheritdoc />
    public void OnPaused(IDownloadTask task) => _paused.OnNext(task);

    /// <inheritdoc />
    public void OnResumed(IDownloadTask task) => _resumed.OnNext(task);

    /// <inheritdoc />
    public Size GetThroughput() => Downloads.SelectMany(x => x.DownloadJobs).GetTotalThroughput(new DateTimeProvider());

    /// <inheritdoc />
    public async Task FinalizeDownloadAsync(IDownloadTask task, TemporaryPath path, string modName)
    {
        task.Status = DownloadTaskStatus.Installing;

        try
        {
            var analyzed = await _archiveAnalyzer.AnalyzeFileAsync(path);
            _analyzed.OnNext((task, analyzed.Hash, modName));
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

    private void UpdateAsComplete(IDownloadTask task)
    {
        // Note: It's easier to re-generate state rather than updating existing instance.
        var state = task.ExportState();
        _store.Delete(new IdVariableLength(EntityCategory.DownloadStates, state.DownloadPath));
        _store.Put(new IdVariableLength(EntityCategory.DownloadStates, state.DownloadPath), state);
    }
}
