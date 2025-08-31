using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Sdk;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="IDownloadsService"/>.
/// </summary>
public sealed class DownloadsService : IDownloadsService, IDisposable
{
    // TODO: Localize this string
    private const string UnknownDownloadName = "Unknown Download";
    
    private readonly IJobMonitor _jobMonitor;
    private readonly IConnection _connection;
    private readonly CompositeDisposable _disposables = new();
    
    // Mapping from JobId to DownloadId to maintain consistency
    private readonly ConcurrentDictionary<JobId, DownloadId> _jobIdToDownloadId = new();
    
    private readonly SourceCache<DownloadInfo, DownloadId> _downloadCache = new(x => x.Id);
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public DownloadsService(
        IJobMonitor jobMonitor,
        IConnection connection)
    {
        _jobMonitor = jobMonitor;
        _connection = connection;

        InitializeObservables();
    }
    
    private void InitializeObservables()
    {
        // TODO: Move completed download jobs to a field in this class, as we don't persist them in JobMonitor.

        // Monitor Nexus Mods download jobs and transform them into DownloadInfo
        _jobMonitor.GetObservableChangeSet<NexusModsDownloadJob>()
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    var nexusJob = (NexusModsDownloadJob)change.Current.Definition;
                    var info = CreateDownloadInfo(nexusJob, nexusJob.HttpDownloadJob.Job);
                    
                    switch (change.Reason)
                    {
                        case ChangeReason.Add:
                        case ChangeReason.Update:
                        case ChangeReason.Refresh:
                            _downloadCache.AddOrUpdate(info);
                            break;
                        case ChangeReason.Remove:
                            _downloadCache.RemoveKey(info.Id);
                            break;
                    }
                }
            })
            .DisposeWith(_disposables);
            
        // Update download info with a 0.5s poll.
        // Note(sewer): We're polling by choice here, in the interest of efficiency.
        // Some items, e.g. transfer rate, amount downloaded, can change constantly; at unpredictable intervals.
        // Likewise, others, are not inherently observable.
        _jobMonitor.GetObservableChangeSet<NexusModsDownloadJob>()
            .AutoRefreshOnObservable(_ => Observable.Interval(TimeSpan.FromMilliseconds(500)))
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    if (change.Reason is not (ChangeReason.Refresh or ChangeReason.Update))
                        continue;

                    // Look up the DownloadId for this JobId
                    if (_jobIdToDownloadId.TryGetValue(change.Current.Id, out var downloadId))
                    {
                        if (_downloadCache.Lookup(downloadId).HasValue)
                        {
                            var nexusJob = (NexusModsDownloadJob)change.Current.Definition;
                            UpdateDownloadInfo(_downloadCache.Lookup(downloadId).Value, nexusJob, nexusJob.HttpDownloadJob.Job);
                        }
                    }
                }
            })
            .DisposeWith(_disposables);
    }
    
    // Observable properties implementation
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> ActiveDownloads => 
        _downloadCache.Connect()
            .AutoRefresh(x => x.Status)
            .Filter(x => x.Status.IsActive());
    
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> CompletedDownloads =>
        _downloadCache.Connect()
            .AutoRefresh(x => x.Status)
            .Filter(x => x.Status == JobStatus.Completed);
    
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> AllDownloads =>
        _downloadCache.Connect();
    
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> GetDownloadsForGame(GameId gameId) =>
        _downloadCache.Connect()
            .Filter(x => x.GameId.Equals(gameId));
    
    
    // Control operations
    public void PauseDownload(DownloadInfo downloadInfo) 
    {
        if (downloadInfo.JobId.HasValue)
            _jobMonitor.Pause(downloadInfo.JobId.Value);
    }
    
    public void ResumeDownload(DownloadInfo downloadInfo) 
    {
        if (downloadInfo.JobId.HasValue)
            _jobMonitor.Resume(downloadInfo.JobId.Value);
    }
    
    public void CancelDownload(DownloadInfo downloadInfo) 
    {
        if (downloadInfo.JobId.HasValue)
            _jobMonitor.Cancel(downloadInfo.JobId.Value);
    }
    public void PauseAll()
    {
        foreach (var download in _downloadCache.Items)
        {
            if (download.Status == JobStatus.Running && download.JobId.HasValue)
                _jobMonitor.Pause(download.JobId.Value);
        }
    }

    public void ResumeAll()
    {
        foreach (var download in _downloadCache.Items)
        {
            if (download.Status == JobStatus.Paused && download.JobId.HasValue)
                _jobMonitor.Resume(download.JobId.Value);
        }
    }
    public void CancelRange(IEnumerable<DownloadInfo> downloads)
    {
        foreach (var download in downloads)
        {
            if (download.JobId.HasValue)
                _jobMonitor.Cancel(download.JobId.Value);
        }
    }
    
    private DownloadInfo CreateDownloadInfo(NexusModsDownloadJob nexusJob, IJob httpDownloadJob)
    {
        // Check if we already have a DownloadId for this JobId, otherwise generate a new one
        var downloadId = _jobIdToDownloadId.GetOrAdd(httpDownloadJob.Id, _ => DownloadId.New());
        
        var info = new DownloadInfo 
        { 
            Id = downloadId,
            JobId = Optional<JobId>.Create(httpDownloadJob.Id),
            GameId = nexusJob.FileMetadata.Uid.GameId,
        };
        PopulateDownloadInfo(info, nexusJob, httpDownloadJob);
        return info;
    }
    
    private void UpdateDownloadInfo(DownloadInfo info, NexusModsDownloadJob nexusJob, IJob httpDownloadJob) => PopulateDownloadInfo(info, nexusJob, httpDownloadJob);

    private void PopulateDownloadInfo(DownloadInfo info, NexusModsDownloadJob nexusJob, IJob httpDownloadJob)
    {
        var httpJobDefinition = nexusJob.HttpDownloadJob.JobDefinition;
        
        info.Name = ExtractName(nexusJob);
        var state = httpDownloadJob.GetJobStateData<IHttpDownloadState>();
        info.FileSize = GetFileSize(state);
        info.Progress = httpDownloadJob.Progress.HasValue ? httpDownloadJob.Progress.Value : Percent.Zero;
        info.DownloadedBytes = GetDownloadedBytes(state);
        info.TransferRate = Size.FromLong((long)(httpDownloadJob.RateOfProgress.HasValue ? httpDownloadJob.RateOfProgress.Value : 0));
        info.Status = httpDownloadJob.Status;
        info.DownloadPageUri = httpJobDefinition.DownloadPageUri;
        info.CompletedAt = httpDownloadJob.Status == JobStatus.Completed ? DateTimeOffset.UtcNow : Optional<DateTimeOffset>.None;
    }
    
    // Helper methods

    private string ExtractName(NexusModsDownloadJob nexusJob)
    {
        // Direct access to file name from FileMetadata
        var fileName = nexusJob.FileMetadata.Name;
        if (!string.IsNullOrEmpty(fileName))
            return fileName;
        
        // Note(sewer): The name should never be empty in practice, as we always fetch the metadata before
        // starting a download, however; as a precaution; we provide a fallback here. This fallback
        // should also work for non-Nexus downloads in the future.

        // Fallback to destination filename if FileMetadata.Name is empty as absolute last resort.
        var httpJob = nexusJob.HttpDownloadJob.JobDefinition;
        if (httpJob.Destination == default(AbsolutePath))
            return UnknownDownloadName;

        var destinationFileName = httpJob.Destination.FileName;
        if (string.IsNullOrEmpty(destinationFileName))
            return UnknownDownloadName;
        
        var nameWithoutExt = Path.GetFileNameWithoutExtension(destinationFileName);
        return nameWithoutExt.Replace('_', ' ').Replace('-', ' ');
    }
    
    private Size GetFileSize(IHttpDownloadState? state)
    {
        return state?.ContentLength.HasValue == true 
            ? state.ContentLength.Value :
            // If content length not available, use downloaded bytes
            // This can happen on some HTTP servers.
            state?.TotalBytesDownloaded ?? Size.Zero;
    }
    
    private Size GetDownloadedBytes(IHttpDownloadState? state) => state?.TotalBytesDownloaded ?? Size.Zero;

    public Optional<LibraryFile.ReadOnly> ResolveLibraryFile(DownloadInfo downloadInfo)
    {
        // Only resolve for completed downloads
        if (downloadInfo.Status != JobStatus.Completed)
            return Optional<LibraryFile.ReadOnly>.None;
            
        // Only resolve if we have a valid JobId
        if (!downloadInfo.JobId.HasValue)
            return Optional<LibraryFile.ReadOnly>.None;
        
        // Find the original job from the job monitor
        var job = _jobMonitor.Jobs.FirstOrDefault(j => j.Id == downloadInfo.JobId.Value);
        if (job?.Definition is not NexusModsDownloadJob nexusJob)
            return Optional<LibraryFile.ReadOnly>.None;
        
        try
        {
            // Use the file metadata directly from the job
            var fileMetadata = nexusJob.FileMetadata;
            
            // Find library items that match this file metadata
            var libraryItems = NexusModsLibraryItem.FindByFileMetadata(_connection.Db, fileMetadata);
            if (libraryItems.Count == 0)
                return Optional<LibraryFile.ReadOnly>.None;
            
            // Convert the first library item to LibraryFile.ReadOnly
            var libraryItem = libraryItems.First().AsLibraryItem();
            return !libraryItem.TryGetAsLibraryFile(out var libraryFile) 
                ? Optional<LibraryFile.ReadOnly>.None
                : Optional<LibraryFile.ReadOnly>.Create(libraryFile);
        }
        catch (Exception)
        {
            // Any database error results in None
            return Optional<LibraryFile.ReadOnly>.None;
        }
    }

    
    public void Dispose()
    {
        _disposables.Dispose();
        _downloadCache.Dispose();
    }
}
