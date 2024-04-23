using System.ComponentModel;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    protected DownloaderState.Model? PersistentState = null;
    protected HttpDownloaderState? TransientState = null!;
    protected ILogger<ADownloadTask> Logger;
    protected TemporaryFileManager TemporaryFileManager;
    protected HttpClient HttpClient;
    protected IHttpDownloader HttpDownloader;
    protected CancellationTokenSource CancellationTokenSource;
    protected TemporaryPath DownloadLocation;

    public Size Downloaded => State!.Downloaded;
    
    public Percent Progress
    {
        get
        {
            if (State!.Size == Size.Zero || Downloaded == Size.Zero)
                return Percent.Zero;
            return Percent.CreateClamped((long)Downloaded.Value, (long)State.Size.Value);
        }
    }

    protected ADownloadTask(IServiceProvider provider)
    {
        _conn = provider.GetRequiredService<IConnection>();
        Logger = provider.GetRequiredService<ILogger<ADownloadTask>>();
        TemporaryFileManager = provider.GetRequiredService<TemporaryFileManager>();
        HttpClient = provider.GetRequiredService<HttpClient>();
        HttpDownloader = provider.GetRequiredService<IHttpDownloader>();
        CancellationTokenSource = new CancellationTokenSource();
    }
    
    protected async Task<(string Name, Size Size)> GetNameAndSizeAsync(Uri uri)
    {
        if (uri.IsFile)
            return (uri.FileName, uri.FileName.Size);

        var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("HTTP request {Url} failed with status {ResponseStatusCode}", uri, response.StatusCode);
            return default;
        }

        // Get the filename from the Content-Disposition header, or default to a temporary file name.
        var contentDispositionHeader = response.Content.Headers.ContentDisposition?.FileNameStar
                                       ?? response.Content.Headers.ContentDisposition?.FileName
                                       ?? Path.GetTempFileName();

        var name = contentDispositionHeader.Trim('"');
        var size = Size.From((ulong)response.Content.Headers.ContentLength.GetValueOrDefault(0));
        return (name, size);
    }
    
    protected async Task SetStatus(DownloadTaskStatus status)
    {
        using var tx = _conn.BeginTransaction();
        tx.Add(State!.Id, DownloaderState.Status, (byte)status);
        
        if (TransientState != null)
        {
            var downloaded = TransientState.ActivityStatus?.MakeTypedReport().Current.Value ?? Size.Zero;
            tx.Add(State.Id, DownloaderState.Downloaded, downloaded);
        }
        
        await tx.Commit();
    }
    
    /// <inheritdoc />
    public Bandwidth CalculateThroughput()
    {
        if (TransientState!.Activity == null)
            return Bandwidth.From(0);

        var report = TransientState.ActivityStatus?.GetReport() 
            as ActivityReport<Size>;
        var size = report?
            .Throughput
            .ValueOr(() => Size.Zero) ?? Size.Zero;

        return Bandwidth.From(size.Value);
    }

    public DownloaderState.Model State => PersistentState!;
    
    public abstract Task StartAsync();
    public abstract void Cancel();
    public abstract void Suspend();
    public abstract Task Resume();
    public abstract Task AddInitData(ITransaction tx, DownloaderState.Model state);
}
