﻿using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.CLI.Types.DownloadHandlers;

/// <summary>
/// Handler for downloading from a NXM protocol.
/// </summary>
public class NxmDownloadProtocolHandler : IDownloadProtocolHandler
{
    private readonly Client _client;
    private readonly IHttpDownloader _downloader;
    private readonly TemporaryFileManager _temp;
    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly IArchiveInstaller _archiveInstaller;

    /// <summary/>
    public NxmDownloadProtocolHandler(Client client, 
        IHttpDownloader downloader, TemporaryFileManager temp, 
        IArchiveInstaller archiveInstaller, IArchiveAnalyzer archiveAnalyzer)
    {
        _archiveAnalyzer = archiveAnalyzer;
        _archiveInstaller = archiveInstaller;
        _client = client;
        _downloader = downloader;
        _temp = temp;
    }

    /// <inheritdoc />
    public string Protocol => "nxm";

    /// <inheritdoc />
    public async Task Handle(string url, LoadoutMarker loadout, string modName, CancellationToken token)
    {
        await using var tempPath = _temp.CreateFile();
        var parsed = NXMUrl.Parse(url);
        
        // Note: Normally we should probably source domains from the loadout, but in this case, this is okay.
        Response<DownloadLink[]> links;
        if (parsed.Key == null)
            links = await _client.DownloadLinksAsync(parsed.Mod.Game, parsed.Mod.ModId, parsed.Mod.FileId, token);
        else
            links = await _client.DownloadLinksAsync(parsed.Mod.Game, parsed.Mod.ModId, parsed.Mod.FileId, parsed.Key.Value, parsed.ExpireTime!.Value, token);

        var downloadUris = links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray();
        
        await _downloader.DownloadAsync(downloadUris, tempPath, null, null, token);
        var hash = await _archiveAnalyzer.AnalyzeFileAsync(tempPath, token);
        await _archiveInstaller.AddMods(loadout.Value.LoadoutId, hash.Hash, modName, token:token);
    }
}
