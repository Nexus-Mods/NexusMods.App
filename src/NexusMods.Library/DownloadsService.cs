using System.Diagnostics;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Resources;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.NexusModsApi;
using R3;
using ReactiveUI;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="IDownloadsService"/>.
/// </summary>
public sealed class DownloadsService : IDownloadsService, IDisposable
{
    private readonly IJobMonitor _jobMonitor;
    private readonly IConnection _connection;
    private readonly System.Reactive.Disposables.CompositeDisposable _disposables = new();
    
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
        // Note(sewer): 
        _jobMonitor.GetObservableChangeSet<INexusModsDownloadJob>()
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
                                var nexusJob = (INexusModsDownloadJob)change.Current.Definition;
                                var httpDownloadJob = nexusJob.HttpDownloadJob.Job;
                                var downloadInfo = CreateDownloadInfo(nexusJob, change.Current.Id);
                                updater.AddOrUpdate(downloadInfo, change.Current.Id);
                                
                                // Subscribe to job observables for reactive updates
                                if (change.Reason == ChangeReason.Add)
                                    SubscribeToJobObservables(httpDownloadJob, downloadInfo);

                                break;
                            case ChangeReason.Remove:
                                // Note(sewer): JobMonitor removes all jobs after completion, but we want to keep the completed jobs
                                //              to show in the 'Completed' tab.
                                //              Cancelled jobs can also yield a ChangeReason.Remove, so we need to make a distinction here. 
                                if (change.Current.Status == JobStatus.Completed)
                                {
                                    // Keep completed downloads but mark them as completed
                                    var existingItem = updater.Lookup(change.Key);
                                    if (existingItem.HasValue)
                                    {
                                        var completedDownload = existingItem.Value;
                                        MarkJobAsCompleted(completedDownload);
                                        // Clean up observable subscriptions
                                        completedDownload.Subscriptions?.Dispose();
                                    }
                                }
                                else
                                {
                                    // Remove non-completed downloads normally
                                    var existingItem = updater.Lookup(change.Key);
                                    if (existingItem.HasValue)
                                        existingItem.Value.Subscriptions?.Dispose();

                                    updater.RemoveKey(change.Key);
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
            .FilterOnObservable(x => x.Status.AsObservable().Select(status => status.IsActive()).AsSystemObservable())
            .RefCount();
    
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> CompletedDownloads =>
        _downloadCache.Connect()
            .FilterOnObservable(x => x.Status.AsObservable().Select(status => status == JobStatus.Completed).AsSystemObservable())
            .RefCount();
    
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> AllDownloads =>
        _downloadCache.Connect();
    
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> GetDownloadsForGame(GameId gameId) =>
        _downloadCache.Connect()
            .Filter(x => x.GameId.Value.Equals(gameId));
    
    public IObservable<IChangeSet<DownloadInfo, DownloadId>> GetActiveDownloadsForGame(GameId gameId) =>
        _downloadCache.Connect()
            .FilterOnObservable(x => x.Status.AsObservable().Select(status => x.GameId.Value.Equals(gameId) && status.IsActive()).AsSystemObservable())
            .RefCount();
    
    /// <summary>
    /// Helper method to resolve a <see cref="DownloadInfo.Id"/> ID to the underlying <see cref="HttpDownloadJob"/> ID.
    /// This is a temporary workaround until the job system properly delegates capabilities 
    /// (tracked in issue #3892).
    /// </summary>
    /// <param name="downloadInfo">The download info containing the NexusModsDownloadJob ID</param>
    /// <returns>The ID of the underlying HttpDownloadJob if found, otherwise the original ID</returns>
    private JobId ResolveToHttpDownloadJobId(DownloadInfo downloadInfo)
    {
        // Try to find the job in the job monitor
        var job = _jobMonitor.Find(downloadInfo.Id);
        if (job == null)
            return downloadInfo.Id;
        
        // Try to cast the job definition to INexusModsDownloadJob and return inner HttpDownloadJob if possible.
        if (job.Definition is INexusModsDownloadJob nexusJob)
            return nexusJob.HttpDownloadJob.Job.Id;
        
        return downloadInfo.Id;
    }
    
    // Control operations
    
    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void PauseDownload(DownloadInfo downloadInfo) => _jobMonitor.Pause(ResolveToHttpDownloadJobId(downloadInfo));
    
    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void ResumeDownload(DownloadInfo downloadInfo) => _jobMonitor.Resume(ResolveToHttpDownloadJobId(downloadInfo));
    
    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void CancelDownload(DownloadInfo downloadInfo) => _jobMonitor.Cancel(ResolveToHttpDownloadJobId(downloadInfo));
    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void PauseAll()
    {
        foreach (var download in _downloadCache.Items.Where(d => d.Status.Value == JobStatus.Running))
            _jobMonitor.Pause(ResolveToHttpDownloadJobId(download));
    }

    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void PauseAllForGame(GameId gameId)
    {
        foreach (var download in _downloadCache.Items.Where(d => 
            d.Status.Value == JobStatus.Running && d.GameId.Value.Equals(gameId)))
            _jobMonitor.Pause(ResolveToHttpDownloadJobId(download));
    }

    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void ResumeAll()
    {
        foreach (var download in _downloadCache.Items.Where(d => d.Status.Value == JobStatus.Paused))
            _jobMonitor.Resume(ResolveToHttpDownloadJobId(download));
    }

    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void ResumeAllForGame(GameId gameId)
    {
        foreach (var download in _downloadCache.Items.Where(d => 
            d.Status.Value == JobStatus.Paused && d.GameId.Value.Equals(gameId)))
            _jobMonitor.Resume(ResolveToHttpDownloadJobId(download));
    }
    
    // Note(sewer) Workaround for issue #3892: Resolve to the underlying HttpDownloadJob ID
    public void CancelRange(IEnumerable<DownloadInfo> downloads)
    {
        foreach (var download in downloads)
            _jobMonitor.Cancel(ResolveToHttpDownloadJobId(download));
    }
    
    private DownloadInfo CreateDownloadInfo(INexusModsDownloadJob nexusJob, JobId currentId)
    {
        var httpJobDefinition = nexusJob.HttpDownloadJob.JobDefinition;
        
        var info = new DownloadInfo 
        { 
            Id = currentId,
        };
        
        // Set initial values using internal methods
        info.SetGameId(nexusJob.FileMetadata.Uid.GameId);
        info.SetName(ExtractName(nexusJob));
        info.SetDownloadPageUri(httpJobDefinition.DownloadPageUri);
        info.SetFileMetadataId(nexusJob.FileMetadata.Id);
        // FileSize, Progress, DownloadedBytes, TransferRate, Status, CompletedAt are set by observable subscriptions
        
        return info;
    }

    private void SubscribeToJobObservables(IJob httpDownloadJob, DownloadInfo downloadInfo)
    {
        // Ensure we don't have existing subscriptions for this job
        downloadInfo.Subscriptions?.Dispose();
        
        var jobDisposables = new System.Reactive.Disposables.CompositeDisposable();
        
        // Subscribe to progress changes
        httpDownloadJob.ObservableProgress
            .Subscribe(progress => downloadInfo.SetProgress(progress.HasValue ? progress.Value : Percent.Zero))
            .DisposeWith(jobDisposables);
        
        // Subscribe to rate of progress changes
        httpDownloadJob.ObservableRateOfProgress
            .Subscribe(rateOfProgress => downloadInfo.SetTransferRate(Size.FromLong((long)(rateOfProgress.HasValue ? rateOfProgress.Value : 0))))
            .DisposeWith(jobDisposables);
        
        // Subscribe to status changes
        httpDownloadJob.ObservableStatus
            .Subscribe(status => downloadInfo.SetStatus(status))
            .DisposeWith(jobDisposables);
        
        // Subscribe to reactive properties from IHttpDownloadState
        var state = httpDownloadJob.GetJobStateData<IHttpDownloadState>();
        Debug.Assert(state != null, "IHttpDownloadState should always exist for HttpDownloadJob");
        
        // Subscribe to ContentLength changes (FileSize)
        state.WhenAnyValue(x => x.ContentLength)
            .Subscribe(contentLength => downloadInfo.SetFileSize(contentLength.HasValue ? contentLength.Value : Size.From(0)))
            .DisposeWith(jobDisposables);

        // Subscribe to TotalBytesDownloaded changes (DownloadedBytes)
        state.WhenAnyValue(x => x.TotalBytesDownloaded)
            .Subscribe(totalBytes => downloadInfo.SetDownloadedBytes(totalBytes))
            .DisposeWith(jobDisposables);
        
        // Store the subscription for later disposal
        downloadInfo.Subscriptions = jobDisposables;
    }

    private static void MarkJobAsCompleted(DownloadInfo downloadInfo)
    {
        // Reset transient properties that are only relevant for active downloads
        downloadInfo.SetTransferRate(Size.From(0));
        
        // Set completion timestamp
        downloadInfo.SetCompletedAt(DateTimeOffset.UtcNow);
        
        // Keep Progress at 100% and other completed state
    }

    // Helper methods
    private string ExtractName(INexusModsDownloadJob nexusJob)
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
            return Language.Downloads_UnknownDownload;

        var destinationFileName = httpJob.Destination.FileName;
        if (string.IsNullOrEmpty(destinationFileName))
            return Language.Downloads_UnknownDownload;
        
        var nameWithoutExt = Path.GetFileNameWithoutExtension(destinationFileName);
        return nameWithoutExt.Replace('_', ' ').Replace('-', ' ');
    }

    public Optional<LibraryFile.ReadOnly> ResolveLibraryFile(DownloadInfo downloadInfo)
    {
        // Only resolve for completed downloads
        if (downloadInfo.Status.Value != JobStatus.Completed)
            return Optional<LibraryFile.ReadOnly>.None;
        
        try
        {
            // Retrieve the FileMetadata from the database using the stored EntityId
            var fileMetadata = new NexusModsFileMetadata.ReadOnly(_connection.Db, downloadInfo.FileMetadataId.Value);
            
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
