using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.App.Downloaders;

/// <summary>
/// Represents an individual task to download and install a .nxm link.
/// </summary>
/// <remarks>
///     This task is usually created via <see cref="DownloadService.AddNxmTask"/>.
/// </remarks>
public class NxmDownloadTask : IDownloadTask
{
    private NXMUrl _url = null!;
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly TemporaryFileManager _temp;
    private readonly Client _client;
    private readonly IHttpDownloader _downloader;
    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly CancellationTokenSource _tokenSource;
    private readonly HttpDownloaderState _state;
    private Task? _task;

    /// <inheritdoc />
    public IEnumerable<IJob<Size>> DownloadJobs => _state.Jobs;

    /// <inheritdoc />
    public DownloadService Owner { get; }

    /// <summary/>
    /// <remarks>
    ///     This constructor is intended to be called from Dependency Injector.
    ///     After running this constructor, you will need to run 
    /// </remarks>
    public NxmDownloadTask(LoadoutRegistry loadoutRegistry, TemporaryFileManager temp, Client client, IHttpDownloader downloader, IArchiveAnalyzer archiveAnalyzer, IArchiveInstaller archiveInstaller, DownloadService owner)
    {
        _loadoutRegistry = loadoutRegistry;
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
    public void Init(NXMUrl url) => _url = url;
    
    public Task StartAsync()
    {
        _task = StartImpl();
        return _task;
    }
    
    private async Task StartImpl()
    {
        // TODO: Some error handling here in case user does not have correct game.
        var token = _tokenSource.Token;
        await using var tempPath = _temp.CreateFile();
        var loadout = _loadoutRegistry.AllLoadouts().First(x => x.Installation.Game.Domain == _url.Mod.Game);
        
        Response<DownloadLink[]> links;
        if (_url.Key == null)
            links = await _client.DownloadLinksAsync(_url.Mod.Game, _url.Mod.ModId, _url.Mod.FileId, token);
        else
            links = await _client.DownloadLinksAsync(_url.Mod.Game, _url.Mod.ModId, _url.Mod.FileId, _url.Key.Value, _url.ExpireTime!.Value, token);
        
        var downloadUris = links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray();
        var file = (await _client.ModFilesAsync(_url.Mod.Game, _url.Mod.ModId, token))
            .Data.Files.First(x => x.FileId == _url.Mod.FileId);
        await _downloader.DownloadAsync(downloadUris, tempPath, _state, Size.FromLong(file.SizeInBytes!.Value), token);

        var analyzed = await _archiveAnalyzer.AnalyzeFileAsync(tempPath, token:token);
        await _archiveInstaller.AddMods(loadout.LoadoutId, analyzed.Hash, file.Name, token);
        Owner.OnComplete(this);
    }

    public void Cancel()
    {
        try { _tokenSource.Cancel(); }
        catch (Exception) { /* ignored */ }
        try { _task?.Wait(); }
        catch (Exception) { /* ignored */ }
        Owner.OnCancelled(this);
    }

    public void Pause() => throw new NotImplementedException();
    public void Resume() => throw new NotImplementedException();
}
