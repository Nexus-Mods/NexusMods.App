using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.App.Downloaders;

/// <summary>
/// This is a 'download manager' of sorts.
/// This service contains all of the downloads which have begun, or have.
/// </summary>
public class DownloadService
{
    /// <summary>
    /// Contains all downloads managed by the application.
    /// </summary>
    public List<IDownloadTask> Downloads { get; } = new();
    
    private readonly IServiceProvider _provider;
    private readonly Subject<IDownloadTask> _started = new();
    private readonly Subject<IDownloadTask> _completed = new();
    private readonly Subject<IDownloadTask> _cancelled = new();
    private readonly Subject<IDownloadTask> _paused = new();
    private readonly Subject<IDownloadTask> _resumed = new();
    
    public DownloadService(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    /// <summary>
    /// This gets fired whenever a download-and-install task is started.
    /// </summary>
    public IObservable<IDownloadTask> StartedTasks => _started;

    /// <summary>
    /// This gets fired whenever a status of download-and-install task is completed.
    /// This happens when <see cref="JobState.Finished"/> is true.
    /// </summary>
    public IObservable<IDownloadTask> CompletedTasks => _completed;

    /// <summary>
    /// This gets fired whenever a status of download-and-install task is completed.
    /// This happens when <see cref="JobState.Finished"/> is true.
    /// </summary>
    public IObservable<IDownloadTask> CancelledTasks => _cancelled;

    /// <summary>
    /// This gets fired whenever a download-and-install task is paused.
    /// This happens when <see cref="JobState.Paused"/> is true.
    /// </summary>
    public IObservable<IDownloadTask> PausedTasks => _paused;

    /// <summary>
    /// This gets fired whenever a download-and-install task is resumed.
    /// This happens when <see cref="JobState.Running"/> is true after <see cref="JobState.Paused"/>.
    /// </summary>
    public IObservable<IDownloadTask> ResumedTasks => _resumed;

    /// <summary>
    /// Adds a task that will download from a NXM link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    public void AddNxmTask(NXMUrl url)
    {
        var task = _provider.GetRequiredService<NxmDownloadTask>();
        task.Init(url);
        AddTask(task);
    }

    /// <summary>
    /// Adds a task that will download from a HTTP link.
    /// </summary>
    /// <param name="url">Url to download from.</param>
    /// <param name="loadout">Loadout for the task.</param>
    public void AddHttpTask(string url, Loadout loadout)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        task.Init(url, loadout);
        AddTask(task);
    }
    
    /// <summary>
    /// Adds a task to the download queue.
    /// </summary>
    /// <param name="task">A task which has not yet been started.</param>
    public void AddTask(IDownloadTask task)
    {
        Downloads.Add(task);
        _ = task.StartAsync();
        _started.OnNext(task);
    }
    
    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    public void OnComplete(IDownloadTask task)
    {
        _completed.OnNext(task);
    }
    
    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    public void OnCancelled(IDownloadTask task)
    {
        _cancelled.OnNext(task);
    }
    
    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    public void OnPaused(IDownloadTask task)
    {
        _paused.OnNext(task);
    }
    
    /// <summary>
    /// This is a callback fired by individual implementations of <see cref="IDownloadTask"/>.
    /// Fires off the necessary events.
    /// </summary>
    public void OnResumed(IDownloadTask task)
    {
        _resumed.OnNext(task);
    }

    /// <summary>
    /// Gets the total throughput of all download operations in bytes per second.
    /// </summary>
    public Size GetThroughput() => Downloads.SelectMany(x => x.DownloadJobs).GetTotalThroughput(new DateTimeProvider());
}
