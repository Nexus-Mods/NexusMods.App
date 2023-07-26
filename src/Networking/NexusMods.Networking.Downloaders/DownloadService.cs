using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc />
public partial class DownloadService : IDownloadService
{
    /// <inheritdoc />
    public List<IDownloadTask> Downloads { get; } = new();

    private readonly ILogger<DownloadService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IDataStore _store;

    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly IArchiveInstaller _archiveInstaller;

    private readonly Subject<IDownloadTask> _started = new();
    private readonly Subject<IDownloadTask> _completed = new();
    private readonly Subject<IDownloadTask> _cancelled = new();
    private readonly Subject<IDownloadTask> _paused = new();
    private readonly Subject<IDownloadTask> _resumed = new();
    private LoadoutRegistry _registry;

    public DownloadService(ILogger<DownloadService> logger, IServiceProvider provider, IDataStore store, IArchiveAnalyzer archiveAnalyzer, IArchiveInstaller archiveInstaller, LoadoutRegistry registry)
    {
        _logger = logger;
        _provider = provider;
        _store = store;
        _archiveAnalyzer = archiveAnalyzer;
        _archiveInstaller = archiveInstaller;
        _registry = registry;

        // TODO: Restore state from DataStore

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
    public void AddNxmTask(NXMUrl url)
    {
        var task = _provider.GetRequiredService<NxmDownloadTask>();
        task.Init(url);
        AddTask(task);
    }

    /// <inheritdoc />
    public void AddHttpTask(string url)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        task.Init(url);
        AddTask(task);
    }

    /// <inheritdoc />
    public void AddTask(IDownloadTask task)
    {
        Downloads.Add(task);
        _ = task.StartAsync();
        _started.OnNext(task);
    }

    /// <inheritdoc />
    public void OnComplete(IDownloadTask task)
    {
        DeleteFromDatastore(task);
        _completed.OnNext(task);
    }

    /// <inheritdoc />
    public void OnCancelled(IDownloadTask task)
    {
        _cancelled.OnNext(task);
    }

    /// <inheritdoc />
    public void OnPaused(IDownloadTask task)
    {
        PersistOnPause(task);
        _paused.OnNext(task);
    }

    /// <inheritdoc />
    public void OnResumed(IDownloadTask task)
    {
        DeleteFromDatastore(task);
        _resumed.OnNext(task);
    }

    /// <inheritdoc />
    public Size GetThroughput() => Downloads.SelectMany(x => x.DownloadJobs).GetTotalThroughput(new DateTimeProvider());

    /// <inheritdoc />
    public async Task FinalizeDownloadAsync(IDownloadTask task, TemporaryPath path, string modName)
    {
        task.Status = DownloadTaskStatus.Installing;

        try
        {
            var analyzed = await _archiveAnalyzer.AnalyzeFileAsync(path);
            await SendToUiAsync(task, analyzed.Hash, modName);
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

    private async Task SendToUiAsync(IDownloadTask task, Hash analyzedHash, string modName)
    {
        var loadouts = Array.Empty<LoadoutId>();
        if (task is IHaveGameDomain gameDomain)
            loadouts = _registry.AllLoadouts().Where(x => x.Installation.Game.Domain == gameDomain.GameDomain).Select(x => x.LoadoutId).ToArray();

        // TODO: This is a placeholder for sending an event to the UI, where the user can choose a layout and confirm mod installation.
        //       For now, we lack the UI design to support this; so we just 'auto install' to whatever we can find.

        // Replace this with sending all possible loadouts to UI, for now we just install whatever we find.
        if (loadouts.Length > 0)
            await _archiveInstaller.AddMods(loadouts[0], analyzedHash, modName);
        else
            await _archiveInstaller.AddMods(_registry.AllLoadouts().First().LoadoutId, analyzedHash, modName);
    }

    private void PersistOnPause(IDownloadTask task)
    {
        var state = task.ExportState();
        _store.Put(new IdVariableLength(EntityCategory.DownloadStates, state.DownloadPath), state);
    }

    private void DeleteFromDatastore(IDownloadTask task)
    {
        _store.Delete(new IdVariableLength(EntityCategory.DownloadStates, task.ExportState().DownloadPath));
    }
}
