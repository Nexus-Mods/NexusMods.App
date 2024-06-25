using System.ComponentModel;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.IO;
using NexusMods.DataModel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Networking.Downloaders.Tasks;

public abstract class ADownloadTask : ReactiveObject, IDownloadTask
{
    private const int PollTimeMilliseconds = 1000;
    
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
    protected TemporaryPath _downloadLocation = default!;
    protected IFileSystem FileSystem;
    protected IFileOriginRegistry FileOriginRegistry;
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
        FileOriginRegistry = provider.GetRequiredService<IFileOriginRegistry>();
    }



    public void Init(DownloaderState.Model state)
    {
        PersistentState = state;
        Downloaded = state.Downloaded;
        _downloadLocation = new TemporaryPath(FileSystem, FileSystem.FromUnsanitizedFullPath(state.DownloadPath), false);
    }


    /// <summary>
    /// Sets up the inital state of the download task, creates the persistent state
    /// and then returns for the parent class to fill out the source information.  
    /// </summary>
    protected EntityId Create(ITransaction tx)
    {
        _downloadLocation = TemporaryFileManager.CreateFile();
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
        Logger.LogDebug("Getting name and size for {Url}", uri);
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
            var report = TransientState.ActivityStatus?.MakeTypedReport();
            if (report == null || !report.Current.HasValue)
            {
                tx.Add(PersistentState.Id, DownloaderState.Downloaded, Size.Zero);
            }
            else
            {
                tx.Add(PersistentState.Id, DownloaderState.Downloaded, report.Current.Value);
            }
        }
        
        var result = await tx.Commit();
        PersistentState = result.Remap(PersistentState);
    }
    
    protected async Task MarkComplete()
    {
        using var tx = Connection.BeginTransaction();
        tx.Add(PersistentState.Id, DownloaderState.Status, (byte)DownloadTaskStatus.Completed);
        tx.Add(PersistentState.Id, CompletedDownloadState.CompletedDateTime, DateTime.Now);
        var result = await tx.Commit();
        PersistentState = result.Remap(PersistentState);
    }
    
    [Reactive]
    public DownloaderState.Model PersistentState { get; set; } = null!;
    
    public AbsolutePath DownloadLocation => _downloadLocation;


    [Reactive] public Bandwidth Bandwidth { get; set; } = Bandwidth.From(0);

    [Reactive] public Size Downloaded { get; set; } = Size.From(0);

    [Reactive] public Percent Progress { get; set; } = Percent.Zero;

    
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
        Logger.LogInformation("Starting download of {Name}", PersistentState.FriendlyName);
        await SetStatus(DownloadTaskStatus.Downloading);
        TransientState = new HttpDownloaderState
        {
            Activity = ActivityFactory.Create<Size>(IHttpDownloader.Group, "Downloading {FileName}", DownloadLocation),
        };
        _ = StartActivityUpdater();
        
        Logger.LogDebug("Dispatching download task for {Name}", PersistentState.FriendlyName);
        await Download(DownloadLocation, CancellationTokenSource.Token);
        UpdateActivity();
        await SetStatus(DownloadTaskStatus.Analyzing);
        Logger.LogInformation("Finished download of {Name} starting analysis", PersistentState.FriendlyName);
        await AnalyzeFile();
        await MarkComplete();
    }

    private async Task AnalyzeFile()
    {
        try
        {
            await FileOriginRegistry.RegisterDownload(DownloadLocation, PersistentState.Id, PersistentState.FriendlyName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to analyze file {Name}", PersistentState.FriendlyName);
        }
    }

    private async Task StartActivityUpdater()
    {
        while (PersistentState.Status == DownloadTaskStatus.Downloading)
        {
            UpdateActivity();
            await Task.Delay(PollTimeMilliseconds);
        }
    }

    private void UpdateActivity()
    {
        try
        {
            var report = TransientState!.ActivityStatus?.MakeTypedReport();
            if (report is { Current.HasValue: true })
            {
                Downloaded = report.Current.Value;
                if (PersistentState.TryGet(DownloaderState.Size, out var size) && size != Size.Zero)
                    Progress = Percent.CreateClamped((long)Downloaded.Value, (long)size.Value);
                if (report.Throughput.HasValue)
                    Bandwidth = Bandwidth.From(report.Throughput.Value.Value);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update activity status");
        }
    }
    
    /// <inheritdoc />
    public void SetIsHidden(bool isHidden, ITransaction tx)
    {
        if (PersistentState.Status != DownloadTaskStatus.Completed) return;
        tx.Add(PersistentState.Id, CompletedDownloadState.Hidden, isHidden);
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
