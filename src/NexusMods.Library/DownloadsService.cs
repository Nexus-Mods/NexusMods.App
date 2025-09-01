using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
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
using ReactiveUI;

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
        // TODO: Restore completed downloads from persistent storage on application boot.

        // Monitor Nexus Mods download jobs and transform them into DownloadInfo
        // Handle completed downloads by keeping them in cache when removed from JobMonitor
        _jobMonitor.GetObservableChangeSet<NexusModsDownloadJob>()
            .Subscribe(changes =>
            {
                _downloadCache.Edit(updater =>
                {
                    foreach (var change in changes)
                    {
                        switch (change.Reason)
                        {
                            case ChangeReason.Add:
                            case ChangeReason.Update:
                            case ChangeReason.Refresh:
                                var nexusJob = (NexusModsDownloadJob)change.Current.Definition;
                                var httpDownloadJob = nexusJob.HttpDownloadJob.Job;
                                var downloadInfo = CreateDownloadInfo(nexusJob, httpDownloadJob);
                                var downloadId = _jobIdToDownloadId.GetOrAdd(change.Current.Id, _ => DownloadId.New());
                                updater.AddOrUpdate(downloadInfo, downloadId);
                                
                                // Subscribe to job observables for reactive updates
                                if (change.Reason == ChangeReason.Add)
                                    SubscribeToJobObservables(httpDownloadJob, downloadInfo);

                                break;
                            case ChangeReason.Remove:
                                // Note(sewer): JobMonitor removes all jobs after completion, but we want to keep the completed jobs
                                //              to show in the 'Completed' tab.
                                //              Cancelled jobs can also yield a ChangeReason.Remove, so we need to make a distinction here. 
                                if (change.Previous.Value.Status == JobStatus.Completed)
                                {
                                    // Keep completed downloads but mark them as completed (clears JobId, etc.)
                                    if (_jobIdToDownloadId.TryGetValue(change.Key, out var completedDownloadId))
                                    {
                                        var existingItem = updater.Lookup(completedDownloadId);
                                        if (existingItem.HasValue)
                                        {
                                            var completedDownload = existingItem.Value;
                                            MarkJobAsCompleted(completedDownload);
                                            // Clean up observable subscriptions
                                            completedDownload.Subscriptions?.Dispose();
                                        }
                                    }
                                }
                                else
                                {
                                    // Remove non-completed downloads normally  
                                    if (_jobIdToDownloadId.TryRemove(change.Key, out var removedDownloadId))
                                    {
                                        var existingItem = updater.Lookup(removedDownloadId);
                                        if (existingItem.HasValue)
                                            existingItem.Value.Subscriptions?.Dispose();

                                        updater.RemoveKey(removedDownloadId);
                                    }
                                }
                                break;
                            case ChangeReason.Moved:
                                // Nothing to do for moves
                                break;
                        }
                    }
                });
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
        var httpJobDefinition = nexusJob.HttpDownloadJob.JobDefinition;
        
        var info = new DownloadInfo 
        { 
            Id = downloadId,
            JobId = Optional<JobId>.Create(httpDownloadJob.Id),
            GameId = nexusJob.FileMetadata.Uid.GameId,
            Name = ExtractName(nexusJob),
            DownloadPageUri = httpJobDefinition.DownloadPageUri,
            // FileSize, Progress, DownloadedBytes, TransferRate, Status, CompletedAt are set by observable subscriptions
        };
        
        return info;
    }

    private void SubscribeToJobObservables(IJob httpDownloadJob, DownloadInfo downloadInfo)
    {
        // Ensure we don't have existing subscriptions for this job
        downloadInfo.Subscriptions?.Dispose();
        
        var jobDisposables = new CompositeDisposable();
        
        // Subscribe to progress changes
        httpDownloadJob.ObservableProgress
            .Subscribe(progress => downloadInfo.Progress = progress.HasValue ? progress.Value : Percent.Zero)
            .DisposeWith(jobDisposables);
        
        // Subscribe to rate of progress changes
        httpDownloadJob.ObservableRateOfProgress
            .Subscribe(rateOfProgress => downloadInfo.TransferRate = Size.FromLong((long)(rateOfProgress.HasValue ? rateOfProgress.Value : 0)))
            .DisposeWith(jobDisposables);
        
        // Subscribe to status changes
        httpDownloadJob.ObservableStatus
            .Subscribe(status => downloadInfo.Status = status)
            .DisposeWith(jobDisposables);
        
        // Subscribe to reactive properties from IHttpDownloadState
        var state = httpDownloadJob.GetJobStateData<IHttpDownloadState>();
        Debug.Assert(state != null, "IHttpDownloadState should always exist for HttpDownloadJob");
        
        // Subscribe to ContentLength changes (FileSize)
        state.WhenAnyValue(x => x.ContentLength)
            .Subscribe(contentLength => downloadInfo.FileSize = contentLength.HasValue ? contentLength.Value : Size.Zero)
            .DisposeWith(jobDisposables);

        // Subscribe to TotalBytesDownloaded changes (DownloadedBytes)
        state.WhenAnyValue(x => x.TotalBytesDownloaded)
            .Subscribe(totalBytes => downloadInfo.DownloadedBytes = totalBytes)
            .DisposeWith(jobDisposables);
        
        // Store the subscription for later disposal
        downloadInfo.Subscriptions = jobDisposables;
    }

    private static void MarkJobAsCompleted(DownloadInfo downloadInfo)
    {
        // Clear the JobId since the job no longer exists in JobMonitor
        downloadInfo.JobId = Optional<JobId>.None;
        
        // Reset transient properties that are only relevant for active downloads
        downloadInfo.TransferRate = Size.Zero;
        
        // Set completion timestamp
        downloadInfo.CompletedAt = DateTimeOffset.UtcNow;
        
        // Keep Progress at 100% and other completed state
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
        // Dispose all job subscriptions
        foreach (var downloadInfo in _downloadCache.Items)
            downloadInfo.Subscriptions?.Dispose();
        
        _disposables.Dispose();
        _downloadCache.Dispose();
    }
}
