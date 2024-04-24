using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc />
public class DownloadService : IDownloadService
{
    /// <inheritdoc />
    public IObservable<IChangeSet<IDownloadTask>> Downloads => throw new NotImplementedException();

    private readonly SourceCache<IDownloadTask, EntityId> _currentDownloads = new(t => t.State.Id);
    private readonly ILogger<DownloadService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IConnection _conn;
    private bool _isDisposed;

    public DownloadService(ILogger<DownloadService> logger, IServiceProvider provider, IConnection conn)
    {
        _logger = logger;
        _provider = provider;
        _conn = conn;
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
        throw new NotImplementedException();
    }
    
    /// <inheritdoc />
    public async Task AddTask(NXMUrl url)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task AddTask(Uri url)
    {
        var task = _provider.GetRequiredService<HttpDownloadTask>();
        await task.Create(url);
        await AddTask(task);
    }

    /// <inheritdoc />
    public async Task AddTask(IDownloadTask task)
    {
        await AddTaskWithoutStarting(task);
        var item = await task.StartAsync();
        return item;
    }

  
    public void Dispose()
    {
        if (_isDisposed)
            return;

        // Pause all tasks, which persists them to datastore.
        foreach (var task in _currentDownloads)
            task.Suspend();

        _isDisposed = true;
    }
}
