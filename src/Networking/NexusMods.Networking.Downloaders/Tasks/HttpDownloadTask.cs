using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tasks;

/// <summary>
/// Represents an individual task to download and install a .nxm link.
/// </summary>
/// <remarks>
///     This task is usually created via <see cref="DownloadService.AddNxmTask"/>.
/// </remarks>
public class HttpDownloadTask : IDownloadTask, IHaveFileSize
{
    private string _url = null!;
    private readonly ILogger<HttpDownloadTask> _logger;
    private readonly TemporaryFileManager _temp;
    private readonly HttpClient _client;
    private readonly IHttpDownloader _downloader;
    private readonly HttpDownloaderState _state;

    private TemporaryPath _downloadLocation; // for resume
    private CancellationTokenSource _tokenSource;
    private Task? _task;

    /// <inheritdoc />
    public IEnumerable<IJob<Size>> DownloadJobs => _state.Jobs;

    /// <inheritdoc />
    public IDownloadService Owner { get; }

    /// <inheritdoc />
    public DownloadTaskStatus Status { get; set; } = DownloadTaskStatus.Idle;

    /// <inheritdoc />
    public string FriendlyName { get; private set; } = "Unknown HTTP Download";

    /// <inheritdoc />
    public long SizeBytes { get; private set; } = -1;

    /// <summary/>
    /// <remarks>
    ///     This constructor is intended to be called from Dependency Injector.
    ///     After running this constructor, you will need to run
    /// </remarks>
    public HttpDownloadTask(ILogger<HttpDownloadTask> logger, TemporaryFileManager temp, HttpClient client, IHttpDownloader downloader, IDownloadService owner)
    {
        _logger = logger;
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
    public void Init(string url) => _url = url;

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
        var tempPath = _temp.CreateFile();
        var nameSize = await GetNameAndSize();
        _downloadLocation = tempPath;
        FriendlyName = nameSize.FileName;
        SizeBytes = nameSize.FileSize;
    }

    private async Task StartOrResumeDownload(TemporaryPath tempPath, CancellationToken token)
    {
        Status = DownloadTaskStatus.Downloading;
        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        await _downloader.DownloadAsync(new[] { request }, tempPath, _state, Size.FromLong(SizeBytes <= 0 ? 0 : SizeBytes), token);
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

    public void Pause()
    {
        Status = DownloadTaskStatus.Paused;
        try { _tokenSource.Cancel(); }
        catch (Exception) { /* ignored */ }

        // Replace the token source.
        _tokenSource = new CancellationTokenSource();
        Owner.OnPaused(this);
    }

    public void Resume()
    {
        _task = ResumeImpl();
        Owner.OnResumed(this);
    }

    public DownloaderState ExportState() => DownloaderState.Create(this, new HttpDownloadState(_url), _downloadLocation.ToString());
    private record GetNameAndSizeResult(string FileName, long FileSize);
}
