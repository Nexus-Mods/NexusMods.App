using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tasks;

/// <summary>
/// Represents an individual task to download and install a .nxm link.
/// </summary>
/// <remarks>
///     This task is usually created via <see cref="DownloadService.AddTask(NexusMods.Abstractions.NexusWebApi.Types.NXMUrl)"/>.
/// </remarks>
public class NxmDownloadTask : ADownloadTask
{
    private readonly INexusApiClient _nexusApiClient;

    public NxmDownloadTask(IServiceProvider provider) : base(provider)
    {
        _nexusApiClient = provider.GetRequiredService<INexusApiClient>();
    }

    /// <summary>
    /// Creates a new download task for the given NXM URL.
    /// </summary>
    internal async Task Create(NXMModUrl nxmUrl)
    {
        using var tx = Connection.BeginTransaction();
        var id = base.Create(tx);

        tx.Add(id, NxmDownloadState.ModId, nxmUrl.ModId);
        tx.Add(id, NxmDownloadState.FileId, nxmUrl.FileId);
        tx.Add(id, NxmDownloadState.Game, nxmUrl.Game);
        tx.Add(id, DownloaderState.GameDomain, GameDomain.From(nxmUrl.Game));
        tx.Add(id, DownloaderState.FriendlyName, "<Unknown>");
        
        if (nxmUrl.ExpireTime.HasValue) 
            tx.Add(id, NxmDownloadState.ValidUntil, nxmUrl.ExpireTime!.Value);
        if (nxmUrl.Key.HasValue)
            tx.Add(id, NxmDownloadState.NxmKey, nxmUrl.Key!.Value.Value);
        
        await Init(tx, id);
    }


    protected override async Task Download(AbsolutePath destination, CancellationToken token)
    {
        Logger.LogInformation("Initializing download links for NXM file {Name}", PersistentState.FriendlyName);
        var links = await InitDownloadLinks(token);

        var foundSize = PersistentState.Contains(DownloaderState.Size);
        if (!foundSize && PersistentState.Contains(NxmDownloadState.ModId))
        {
            Logger.LogInformation("Getting Nexus Mod metadata for NXM file {Name}", PersistentState.FriendlyName);
            foundSize = await UpdateMetadata(token);
        }
        
        if (!foundSize && !PersistentState.Contains(DownloaderState.Size))
        {
            Logger.LogInformation("Getting metadata for NXM file {Name}", PersistentState.FriendlyName);
            await UpdateSizeAndName(links);
        }


        var activity = ((IActivitySource<Size>)TransientState!.Activity!);
        activity.SetMax(PersistentState.Size);

        Logger.LogInformation("Starting download of NXM file {Name}", PersistentState.FriendlyName);
        
        await HttpDownloader.DownloadAsync(links, destination, TransientState, PersistentState.Size, token);
        Logger.LogInformation("Finished download of NXM file {Name}", PersistentState.FriendlyName);
    }

    private async Task<bool> UpdateMetadata(CancellationToken token)
    {
        try
        {
            var nxState = PersistentState.Db.Get<NxmDownloadState.Model>(PersistentState.Id);
            var fileInfos = await _nexusApiClient.ModFilesAsync(nxState.Game, nxState.ModId, token);

            var file = fileInfos.Data.Files.FirstOrDefault(f => f.FileId == nxState.FileId);
            
            var info = await _nexusApiClient.ModInfoAsync(nxState.Game, nxState.ModId, token);


            var eid = PersistentState.Id;
            if (file is { SizeInBytes: not null })
            {
                using var tx = Connection.BeginTransaction();
                if (info.Data.Name is not null)
                    tx.Add(eid, DownloaderState.FriendlyName, info.Data.Name + " " + file.FileName);
                else
                    tx.Add(eid, DownloaderState.FriendlyName, file.FileName);
                
                tx.Add(eid, DownloaderState.Size, Size.FromLong(file.SizeInBytes!.Value));
                tx.Add(eid, DownloaderState.Version, file.Version);
                var result = await tx.Commit();
                PersistentState = result.Db.Get<NxmDownloadState.Model>(eid);
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update metadata for NXM file {Name}", PersistentState.FriendlyName);
            return false;
        }

        return false;

    }

    private async Task UpdateSizeAndName(HttpRequestMessage[] message)
    {
        var (name, size) = await GetNameAndSizeAsync(message.First().RequestUri!);
        using var tx = Connection.BeginTransaction();
        tx.Add(PersistentState.Id, DownloaderState.Size, size);
        tx.Add(PersistentState.Id, DownloaderState.FriendlyName, name);
        var nxState = PersistentState.Db.Get<NxmDownloadState.Model>(PersistentState.Id);
        
        Logger.LogDebug("Updated size and name for {Name} to {Size}", name, size);
        var result = await tx.Commit();
        PersistentState = result.Db.Get<NxmDownloadState.Model>(PersistentState.Id);
    }

    private async Task<HttpRequestMessage[]> InitDownloadLinks(CancellationToken token)
    {
        Response<DownloadLink[]> links;

        var state = PersistentState.Db.Get<NxmDownloadState.Model>(PersistentState.Id);
        
        if (!PersistentState.TryGet(NxmDownloadState.NxmKey, out var key))
            links = await _nexusApiClient.DownloadLinksAsync(state.Game, state.ModId, state.FileId, token);
        else
            links = await _nexusApiClient.DownloadLinksAsync(state.Game, state.ModId, state.FileId, NXMKey.From(state.NxmKey), 
                state.ValidUntil, token);

        return links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray();
    }

   
}
