using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

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
    IObservable<IChangeSet<DownloadInfo, JobId>> ActiveDownloads { get; }
    
    /// <summary>
    /// Observable of all completed downloads.
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, JobId>> CompletedDownloads { get; }
    
    /// <summary>
    /// Observable of all downloads regardless of status.
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, JobId>> AllDownloads { get; }
    
    /// <summary>
    /// Get downloads filtered by game.
    /// </summary>
    IObservable<IChangeSet<DownloadInfo, JobId>> GetDownloadsForGame(GameId gameId);
    
    /// <summary>
    /// Observable of download counts grouped by game.
    /// </summary>
    IObservable<IReadOnlyDictionary<GameId, int>> DownloadCountsByGame { get; }
    
    /// <summary>
    /// Observable of aggregate download speed across all active downloads.
    /// </summary>
    IObservable<Size> AggregateDownloadSpeed { get; }
    
    /// <summary>
    /// Pauses a specific download.
    /// </summary>
    void PauseDownload(JobId jobId);
    
    /// <summary>
    /// Resumes a specific download.
    /// </summary>
    void ResumeDownload(JobId jobId);
    
    /// <summary>
    /// Cancels a specific download.
    /// </summary>
    void CancelDownload(JobId jobId);
    
    /// <summary>
    /// Pauses all active downloads.
    /// </summary>
    void PauseAll();
    
    /// <summary>
    /// Resumes all paused downloads.
    /// </summary>
    void ResumeAll();
    
    /// <summary>
    /// Cancels selected downloads.
    /// </summary>
    void CancelSelected(IEnumerable<JobId> jobIds);
}
