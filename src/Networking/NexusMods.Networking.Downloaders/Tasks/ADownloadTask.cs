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
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Networking.Downloaders.Tasks;

public abstract class ADownloadTask : ReactiveObject, IDownloadTask
{
    protected readonly IConnection Connection;
    protected readonly IActivityFactory ActivityFactory;
    /// <summary>
    /// The state of the download task, persisted to the database, will never
    /// be null after the .Create() method is called.
    /// </summary>
    protected HttpDownloaderState? TransientState = null!;
    protected ILogger<ADownloadTask> Logger;
    protected TemporaryFileManager TemporaryFileManager;
    protected HttpClient HttpClient;
    protected IHttpDownloader HttpDownloader;
    protected CancellationTokenSource CancellationTokenSource;
    protected TemporaryPath DownloadLocation;
    protected IFileSystem FileSystem;
    private DownloaderState.Model _persistentState = null!;

    protected ADownloadTask(IServiceProvider provider)
    {
        Connection = provider.GetRequiredService<IConnection>();
        Logger = provider.GetRequiredService<ILogger<ADownloadTask>>();
        TemporaryFileManager = provider.GetRequiredService<TemporaryFileManager>();
        HttpClient = provider.GetRequiredService<HttpClient>();
        HttpDownloader = provider.GetRequiredService<IHttpDownloader>();
        CancellationTokenSource = new CancellationTokenSource();
        ActivityFactory = provider.GetRequiredService<IActivityFactory>();
        FileSystem = provider.GetRequiredService<IFileSystem>();
    }
    
    public void Init(DownloaderState.Model state)
    {
        PersistentState = state;
        DownloadLocation = new TemporaryPath(FileSystem, FileSystem.FromUnsanitizedFullPath(state.DownloadPath), false);
    }


    /// <summary>
    /// Sets up the inital state of the download task, creates the persistent state
    /// and then returns for the parent class to fill out the source information.  
    /// </summary>
    protected EntityId Create(ITransaction tx)
    {
        DownloadLocation = TemporaryFileManager.CreateFile();
        var state = new DownloaderState.Model(tx)
        {
            Status = DownloadTaskStatus.Idle,
            Downloaded = Size.Zero,
            DownloadPath = DownloadLocation.ToString(),
        };
        return state.Id;
    }

    /// <summary>
    /// Perform the initialisation of the task, this should be called after the
    /// additional metadata has been added to the transaction.
    /// </summary>
    protected async Task Init(ITransaction tx, EntityId id)
    {
        var result = await tx.Commit();
        PersistentState = result.Db.Get<DownloaderState.Model>(result[id]);
    }
    
    protected async Task<(string Name, Size Size)> GetNameAndSizeAsync(Uri uri)
    {
        if (uri.IsFile)
            return default;

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
        using var tx = Connection.BeginTransaction();
        tx.Add(PersistentState.Id, DownloaderState.Status, (byte)status);
        
        if (TransientState != null)
        {
            var downloaded = TransientState.ActivityStatus?.MakeTypedReport().Current.Value ?? Size.Zero;
            tx.Add(PersistentState.Id, DownloaderState.Downloaded, downloaded);
        }
        
        var result = await tx.Commit();
        PersistentState = result.Remap(PersistentState);
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

    [Reactive]
    public DownloaderState.Model PersistentState { get; set; } = null!;

    public Bandwidth Bandwidth { get; set; }
    
    public Size Downloaded { get; set; }
    
    public Percent Progress { get; set; }

    
    /// <inheritdoc />
    public async Task StartAsync()
    {
        await Resume();
    }

    /// <inheritdoc />
    public async Task Cancel()
    {
        try { await CancellationTokenSource.CancelAsync(); }
        catch (Exception) { /* ignored */ }
        await SetStatus(DownloadTaskStatus.Cancelled);
    }

    /// <inheritdoc />
    public async Task Suspend()
    {
        await SetStatus(DownloadTaskStatus.Paused);
        try { await CancellationTokenSource.CancelAsync(); }
        catch (Exception) { /* ignored */ }
        
        // Replace the token source.
        CancellationTokenSource = new CancellationTokenSource();
    }
    
    /// <inheritdoc />
    public async Task Resume()
    {
        await SetStatus(DownloadTaskStatus.Downloading);
        TransientState = new HttpDownloaderState
        {
            Activity = ActivityFactory.Create<Size>(IHttpDownloader.Group, "Downloading {FileName}", DownloadLocation),
        };
        await Download(DownloadLocation, CancellationTokenSource.Token);
        await SetStatus(DownloadTaskStatus.Completed);
    }

    /// <inheritdoc />
    public void ResetState(IDb db)
    {
        PersistentState = db.Get<DownloaderState.Model>(PersistentState.Id);
    }

    /// <summary>
    /// Begin the process of downloading a file to the specified destination, should
    /// terminate when the download is complete or cancelled. The destination may have
    /// vestigial data from a previous download attempt, if the downloader can resume
    /// using this data it should do so. The method should not return until the download
    /// has completed or been cancelled.
    /// </summary>
    protected abstract Task Download(AbsolutePath destination, CancellationToken token);
}
