using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Service for managing and monitoring downloads through the job system.
/// </summary>
[PublicAPI]
public interface IDownloadsService
{
    /// <summary>
    /// Observable of all active downloads (Running or Paused status).
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, DownloadId>> ActiveDownloads { get; }
    
    /// <summary>
    /// Observable of all completed downloads.
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, DownloadId>> CompletedDownloads { get; }
    
    /// <summary>
    /// Observable of all downloads regardless of status.
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, DownloadId>> AllDownloads { get; }
    
    /// <summary>
    /// Get downloads filtered by game.
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, DownloadId>> GetDownloadsForGame(GameId gameId);
    
    /// <summary>
    /// Get active downloads filtered by game.
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, DownloadId>> GetActiveDownloadsForGame(GameId gameId);
    
    
    /// <summary>
    /// Pauses a specific download.
    /// </summary>
    void PauseDownload(DownloadInfo downloadInfo);
    
    /// <summary>
    /// Resumes a specific download.
    /// </summary>
    void ResumeDownload(DownloadInfo downloadInfo);
    
    /// <summary>
    /// Cancels a specific download.
    /// </summary>
    void CancelDownload(DownloadInfo downloadInfo);
    
    /// <summary>
    /// Pauses all active downloads.
    /// </summary>
    void PauseAll();
    
    /// <summary>
    /// Pauses all active downloads for a specific game.
    /// </summary>
    void PauseAllForGame(GameId gameId);
    
    /// <summary>
    /// Resumes all paused downloads.
    /// </summary>
    void ResumeAll();
    
    /// <summary>
    /// Resumes all paused downloads for a specific game.
    /// </summary>
    void ResumeAllForGame(GameId gameId);
    
    /// <summary>
    /// Cancels selected downloads.
    /// </summary>
    void CancelRange(IEnumerable<DownloadInfo> downloads);
    
    /// <summary>
    /// Resolves a library file for a completed download using FileId/GameId matching.
    /// Returns None for incomplete downloads or non-Nexus downloads.
    /// </summary>
    /// <param name="downloadInfo">The download info to resolve library file for.</param>
    /// <returns>The library file if found, otherwise None.</returns>
    Optional<LibraryFile.ReadOnly> ResolveLibraryFile(DownloadInfo downloadInfo);
}
