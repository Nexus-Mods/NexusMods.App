using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tasks;

/// <summary>
/// Represents an individual task to download and install a .nxm link.
/// </summary>
/// <remarks>
///     This task is usually created via <see cref="DownloadService.AddNxmTask"/>.
/// </remarks>
public class HttpDownloadTask : ADownloadTask, IHaveFileSize
{
    private string _url = null!;
    private readonly ILogger<HttpDownloadTask> _logger;
    private readonly TemporaryFileManager _temp;
    private readonly HttpClient _client;
    private readonly IHttpDownloader _downloader;
    private readonly HttpDownloaderState _state;

    private DownloaderState.Model _downloaderState = null!;

    private TemporaryPath _downloadLocation; // for resume
    private CancellationTokenSource _tokenSource;
    private Task? _task;
    private Size _defaultDownloadedSize;
    private readonly IConnection _conn;

    /// <inheritdoc />
    public Size DownloadedSizeBytes => _state.ActivityStatus?.MakeTypedReport().Current.Value ?? _defaultDownloadedSize;

    /// <inheritdoc />
    public EntityId TaskId => _downloaderState.Id;

    /// <inheritdoc />
    public long CalculateThroughput()
    {
        if (_state.Activity == null)
            return 0;

        return (long)(((ActivityReport<Size>?)_state.ActivityStatus?.GetReport())?.Throughput.Value ?? Size.Zero).Value;
    }


    /// <inheritdoc />
    public IDownloadService Owner { get; }

    /// <inheritdoc />
    public DownloadTaskStatus Status => _downloaderState.Status;

    /// <inheritdoc />
    public string FriendlyName => _downloaderState.FriendlyName;

    /// <inheritdoc />
    public Size SizeBytes => _downloaderState.Size;

    /// <summary/>
    /// <remarks>
    ///     This constructor is intended to be called from Dependency Injector.
    ///     After running this constructor, you will need to run
    /// </remarks>
    public HttpDownloadTask(IServiceProvider provider) : base(provider)
    {
        _logger = logger;
        _conn = connection;
        _temp = temp;
        _client = client;
        _downloader = downloader;
        _tokenSource = new CancellationTokenSource();
        _state = new HttpDownloaderState();
        Owner = owner;
    }

    /// <summary>
    /// Initializes components of this task that cannot be DI Injected.
    /// </summary>
    public async Task Init(Uri url)
    {
        using var tx = _conn.BeginTransaction();
        var task = new DownloaderState.Model(tx)
        {
            Status = DownloadTaskStatus.Idle,
            DownloadPath = _temp.CreateFile().ToString(),
            FriendlyName = "Unknown HTTP Download",
            DownloadedBytes = Size.Zero,
        };
        tx.Add(task.Id, HttpDownloadState.Uri, url);
        var result = await tx.Commit();
        _downloaderState = result.Remap(task);
    }

    /// <summary>
    /// Initializes this download from suspended state (after rebooting application or pausing).
    /// After this method is called, please call <see cref="Resume"/>.
    /// </summary>
    public void RestoreFromSuspend(DownloaderState.Model state)
    {
        if (!state.Contains(HttpDownloadState.Uri))
            throw new ArgumentException("Invalid state provided.", nameof(state));

        var absPath = FileSystem.Shared.FromUnsanitizedFullPath(state.DownloadPath);
        _downloadLocation = new TemporaryPath(FileSystem.Shared, absPath);
        _defaultDownloadedSize = state.DownloadedBytes;
    }

    public Task StartAsync()
    {
        _task = StartImpl();
        return _task;
    }

    private async Task StartImpl()
    {
        await InitDownload();
        await ResumeImpl();
    }

    private async Task ResumeImpl()
    {
        var token = _tokenSource.Token;
        await StartOrResumeDownload(_downloadLocation, token);
        await Owner.FinalizeDownloadAsync(this, _downloadLocation, FriendlyName);
    }

    private async Task InitDownload()
    {
        var nameSize = await GetNameAndSize();
        FriendlyName = nameSize.FileName;
        SizeBytes = Size.FromLong(nameSize.FileSize);
        await Owner.UpdatePersistedState(this);
    }

    private async Task StartOrResumeDownload(TemporaryPath tempPath, CancellationToken token)
    {
        Status = DownloadTaskStatus.Downloading;
        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        await _downloader.DownloadAsync(new[] { request }, tempPath, _state, SizeBytes <= Size.Zero ? Size.Zero : SizeBytes, token);
    }

    private async Task<GetNameAndSizeResult> GetNameAndSize()
    {
        var uri = new Uri(_url);
        if (uri.IsFile)
            return new GetNameAndSizeResult(string.Empty, -1);

        var response = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HTTP request {Url} failed with status {ResponseStatusCode}", _url, response.StatusCode);
            return new GetNameAndSizeResult(string.Empty, -1);
        }

        // Get the filename from the Content-Disposition header, or default to a temporary file name.
        var contentDispositionHeader = response.Content.Headers.ContentDisposition?.FileNameStar
                                       ?? response.Content.Headers.ContentDisposition?.FileName
                                       ?? Path.GetTempFileName();

        return new GetNameAndSizeResult(contentDispositionHeader.Trim('"'), response.Content.Headers.ContentLength.GetValueOrDefault(0));
    }

    public void Cancel()
    {
        try { _tokenSource.Cancel(); }
        catch (Exception) { /* ignored */ }

        // Do not _task.Wait() here, as it will deadlock without async.
        Owner.OnCancelled(this);
    }

    public void Suspend()
    {
        Status = DownloadTaskStatus.Paused;
        try { _tokenSource.Cancel(); }
        catch (Exception) { /* ignored */ }

        // Replace the token source.
        _tokenSource = new CancellationTokenSource();
        Owner.OnPaused(this);
    }

    public Task Resume()
    {
        _task = ResumeImpl();
        Owner.OnResumed(this);
        return _task;
    }
    
    private record GetNameAndSizeResult(string FileName, long FileSize);

    #region Test Only
    internal Task StartSuspended()
    {
        _task = StartSuspendedImpl();
        return _task;
    }

    private async Task StartSuspendedImpl()
    {
        await InitDownload();
        Suspend();
    }
    #endregion
}
