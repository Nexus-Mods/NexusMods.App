using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc />
public class DownloadService : IDownloadService
{
    /// <inheritdoc />
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
    public void AddHttpTask(string url, Loadout loadout)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        task.Init(url, loadout);
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
        _paused.OnNext(task);
    }
    
    /// <inheritdoc />
    public void OnResumed(IDownloadTask task)
    {
        _resumed.OnNext(task);
    }

    /// <inheritdoc />
    public Size GetThroughput() => Downloads.SelectMany(x => x.DownloadJobs).GetTotalThroughput(new DateTimeProvider());
}
