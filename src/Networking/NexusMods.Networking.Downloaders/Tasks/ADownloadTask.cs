using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tasks;

public abstract class ADownloadTask : IDownloadTask
{
    private readonly IConnection _conn;
    private Size _downloaded = Size.Zero;
    private DownloaderState.Model? _state = null;
    private HttpDownloaderState? _downloadState = null!;

    public Size Downloaded => State!.Downloaded;

    public ADownloadTask(IServiceProvider provider)
    {
        _conn = provider.GetRequiredService<IConnection>();
        Owner = provider.GetRequiredService<IDownloadService>();
    }
    
    public async Task SetStatus(DownloadTaskStatus status)
    {
        using var tx = _conn.BeginTransaction();
        tx.Add(State!.Id, DownloaderState.Status, (byte)status);
        
        if (_downloadState != null)
        {
            var downloaded = _downloadState.ActivityStatus?.MakeTypedReport().Current.Value ?? Size.Zero;
            tx.Add(State.Id, DownloaderState.Downloaded, downloaded);
        }
        
        await tx.Commit();
    }
    
    /// <inheritdoc />
    public Bandwidth CalculateThroughput()
    {
        if (_downloadState!.Activity == null)
            return Bandwidth.From(0);

        var report = _downloadState.ActivityStatus?.GetReport() 
            as ActivityReport<Size>;
        var size = report?
            .Throughput
            .ValueOr(() => Size.Zero) ?? Size.Zero;

        return Bandwidth.From(size.Value);
    }

    public DownloaderState.Model State => _state!;
    public IDownloadService Owner { get; }

    public abstract Task StartAsync();
    public abstract void Cancel();
    public abstract void Suspend();
    public abstract Task Resume();
}
