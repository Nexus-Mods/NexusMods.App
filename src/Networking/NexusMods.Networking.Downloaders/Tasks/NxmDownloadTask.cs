using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.Downloaders.Interfaces;
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
        var path = TemporaryFileManager.CreateFile();
        var state = new NxmDownloadState.New(tx)
        {
            DownloaderState = new DownloaderState.New(tx)
            {
                GameDomain = GameDomain.From(nxmUrl.Game),
                FriendlyName = "<Unknown>",
                DownloadPath = path.Path.ToString(),
                Status = DownloadTaskStatus.Idle,
            },
            ModId = nxmUrl.ModId,
            FileId = nxmUrl.FileId,
            Game = nxmUrl.Game,
        };
        
        if (nxmUrl.ExpireTime.HasValue) 
            tx.Add(state, NxmDownloadState.ValidUntil, nxmUrl.ExpireTime!.Value);
        if (nxmUrl.Key.HasValue)
            tx.Add(state, NxmDownloadState.NxmKey, nxmUrl.Key!.Value.Value);
        
        await Init(tx, state);
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
        
        var hash = await HttpDownloader.DownloadAsync(links, destination, TransientState, PersistentState.Size, token);
        if (hash.Value == Hash.Zero)
            throw new OperationCanceledException();
        
        Logger.LogInformation("Finished download of NXM file {Name}", PersistentState.FriendlyName);
    }

    private async Task<bool> UpdateMetadata(CancellationToken token)
    {
        try
        {
            if (!PersistentState.TryGetAsNxmDownloadState(out var nxState))
                return false;
            var fileInfos = await _nexusApiClient.ModFilesAsync(nxState.Game, nxState.ModId, token);

            var file = fileInfos.Data.Files.FirstOrDefault(f => f.FileId == nxState.FileId);
            
            var info = await _nexusApiClient.ModInfoAsync(nxState.Game, nxState.ModId, token);


            var eid = PersistentState.Id;
            if (file is { SizeInBytes: not null })
            {
                using var tx = Connection.BeginTransaction();
                if (!string.IsNullOrEmpty(info.Data.Name))
                    tx.Add(eid, DownloaderState.FriendlyName, info.Data.Name);
                else
                    tx.Add(eid, DownloaderState.FriendlyName, file.FileName);
                
                tx.Add(eid, DownloaderState.Size, Size.FromLong(file.SizeInBytes!.Value));
                tx.Add(eid, DownloaderState.Version, file.Version);
                var result = await tx.Commit();
                PersistentState = PersistentState.Rebase(result.Db);
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
        
        Logger.LogDebug("Updated size and name for {Name} to {Size}", name, size);
        var result = await tx.Commit();
        ResetState(result.Db);
    }

    private async Task<HttpRequestMessage[]> InitDownloadLinks(CancellationToken token)
    {
        Response<DownloadLink[]> links;

        if (!PersistentState.TryGetAsNxmDownloadState(out var state))
            throw new InvalidOperationException("State is not a NxmDownloadState");

        if (state.Contains(NxmDownloadState.NxmKey))
            links = await _nexusApiClient.DownloadLinksAsync(state.Game, state.ModId, state.FileId, NXMKey.From(state.NxmKey), 
                state.ValidUntil, token);
        else
            links = await _nexusApiClient.DownloadLinksAsync(state.Game, state.ModId, state.FileId, token);

        return links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray();
    }

   
}
