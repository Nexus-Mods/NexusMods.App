using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

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
    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly CancellationTokenSource _tokenSource;
    private readonly HttpDownloaderState _state;
    private Loadout? _loadout;
    private Task? _task;

    /// <inheritdoc />
    public IEnumerable<IJob<Size>> DownloadJobs => _state.Jobs;

    /// <inheritdoc />
    public DownloadService Owner { get; }

    /// <inheritdoc />
    public DownloadTaskStatus Status { get; private set; } = DownloadTaskStatus.Idle;

    /// <inheritdoc />
    public string FriendlyName { get; private set; } = "Unknown HTTP Download";

    /// <inheritdoc />
    public long SizeBytes { get; private set; } = -1;

    /// <summary/>
    /// <remarks>
    ///     This constructor is intended to be called from Dependency Injector.
    ///     After running this constructor, you will need to run 
    /// </remarks>
    public HttpDownloadTask(ILogger<HttpDownloadTask> logger, TemporaryFileManager temp, HttpClient client, IHttpDownloader downloader, IArchiveAnalyzer archiveAnalyzer, IArchiveInstaller archiveInstaller, DownloadService owner)
    {
        _logger = logger;
        _temp = temp;
        _client = client;
        _downloader = downloader;
        _archiveAnalyzer = archiveAnalyzer;
        _archiveInstaller = archiveInstaller;
        _tokenSource = new CancellationTokenSource();
        _state = new HttpDownloaderState();
        Owner = owner;
    }

    /// <summary>
    /// Initializes components of this task that cannot be DI Injected.
    /// </summary>
    public void Init(string url, Loadout loadout)
    {
        _url = url;
        _loadout = loadout;
    }

    public Task StartAsync()
    {
        _task = StartImpl();
        return _task;
    }
    
    private async Task StartImpl()
    {
        var token = _tokenSource.Token;
        await using var tempPath = _temp.CreateFile();

        Status = DownloadTaskStatus.Downloading;
        var nameSize = await GetNameAndSize();
        FriendlyName = nameSize.FileName;
        SizeBytes = nameSize.FileSize;
        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        await _downloader.DownloadAsync(new[] { request }, tempPath, _state, Size.FromLong(SizeBytes <= 0 ? 0 : SizeBytes), token);
        
        Status = DownloadTaskStatus.Installing;
        var analyzed = await _archiveAnalyzer.AnalyzeFileAsync(tempPath, token:token);
        await _archiveInstaller.AddMods(_loadout!.LoadoutId, analyzed.Hash, nameSize.FileName, token);
        
        Status = DownloadTaskStatus.Completed;
        Owner.OnComplete(this);
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
        try { _task?.Wait(); }
        catch (Exception) { /* ignored */ }
        Owner.OnCancelled(this);
    }

    public void Pause()
    {
        Status = DownloadTaskStatus.Paused;
        throw new NotImplementedException();
    }

    public void Resume() => throw new NotImplementedException();
    private record GetNameAndSizeResult(string FileName, long FileSize);
}
