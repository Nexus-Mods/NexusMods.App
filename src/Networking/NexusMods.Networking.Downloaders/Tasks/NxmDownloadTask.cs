using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Networking.Downloaders.Tasks;

/// <summary>
/// Represents an individual task to download and install a .nxm link.
/// </summary>
/// <remarks>
///     This task is usually created via <see cref="DownloadService.AddNxmTask"/>.
/// </remarks>
public class NxmDownloadTask : IDownloadTask, IHaveDownloadVersion, IHaveFileSize, IHaveGameName, IHaveGameDomain
{
    private NXMUrl _url = null!;
    private readonly TemporaryFileManager _temp;
    private readonly Client _client;
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
    public string FriendlyName { get; private set; } = "Unknown NXM Mod";

    /// <inheritdoc />
    public string Version { get; private set; } = "Unknown Version";

    /// <inheritdoc />
    public string GameName { get; private set; } = "Unknown Game";

    /// <inheritdoc />
    public string GameDomain { get; private set; } = "";

    /// <inheritdoc />
    public long SizeBytes { get; private set; }

    /// <summary/>
    /// <remarks>
    ///     This constructor is intended to be called from Dependency Injector.
    ///     After running this constructor, you will need to run
    /// </remarks>
    public NxmDownloadTask(TemporaryFileManager temp, Client client, IHttpDownloader downloader, IDownloadService owner)
    {
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
    public void Init(NXMUrl url)
    {
        _url = url;
        _downloadLocation = _temp.CreateFile();
    }

    /// <summary>
    /// Initializes this download from suspended state (after rebooting application or pausing).
    /// After this method is called, please call <see cref="Resume"/>.
    /// </summary>
    public void RestoreFromSuspend(DownloaderState state)
    {
        if (state.TypeSpecificData is not NxmDownloadState data)
            throw new ArgumentException("Invalid state provided.", nameof(state));

        var absPath = FileSystem.Shared.FromUnsanitizedFullPath(state.DownloadPath);
        _downloadLocation = new TemporaryPath(FileSystem.Shared, absPath);
        FriendlyName = state.FriendlyName;
        GameName = state.GameName!;
        GameDomain = state.GameDomain!;
        SizeBytes = state.SizeBytes!.Value;
        Version = state.Version!;
        _url = NXMUrl.Parse(data.Query);
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
        await StartOrResumeDownload(token);
        await Owner.FinalizeDownloadAsync(this, _downloadLocation, FriendlyName);
    }

    private async Task StartOrResumeDownload(CancellationToken token)
    {
        Status = DownloadTaskStatus.Downloading;
        var links = await InitDownloadLinks(token);
        await _downloader.DownloadAsync(links, _downloadLocation, _state, Size.FromLong(SizeBytes), token);
    }

    private async Task InitDownload()
    {
        var file = await GetFileInfo();
        GameDomain = _url.Mod.Game;
        FriendlyName = file.Name;
        Version = file.Version;
        SizeBytes = file.SizeInBytes.GetValueOrDefault(-1);
        GameName = _url.Mod.Game;
    }

    private async Task<HttpRequestMessage[]> InitDownloadLinks(CancellationToken token)
    {
        Response<DownloadLink[]> links;
        if (_url.Key == null)
            links = await _client.DownloadLinksAsync(_url.Mod.Game, _url.Mod.ModId, _url.Mod.FileId, token);
        else
            links = await _client.DownloadLinksAsync(_url.Mod.Game, _url.Mod.ModId, _url.Mod.FileId, _url.Key.Value,
                _url.ExpireTime!.Value, token);

        return links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray();
    }

    private async Task<ModFile> GetFileInfo()
    {
        var modFiles = (await _client.ModFilesAsync(_url.Mod.Game, _url.Mod.ModId)).Data;
        var file = modFiles.Files.First(x => x.FileId == _url.Mod.FileId);
        return file;
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

    public DownloaderState ExportState() => DownloaderState.Create(this, new NxmDownloadState(_url.Mod.ToString()), _downloadLocation.ToString());

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
