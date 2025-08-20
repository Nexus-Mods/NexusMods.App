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
    
    private readonly SourceCache<DownloadInfo, JobId> _downloadCache = new(x => x.Id);
    
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
            .Transform(job => {
                var nexusJob = (NexusModsDownloadJob)job.Definition;
                return CreateDownloadInfo(nexusJob, nexusJob.HttpDownloadJob.Job);
            })
            .PopulateInto(_downloadCache)
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

                    if (_downloadCache.Lookup(change.Current.Id).HasValue)
                    {
                        var nexusJob = (NexusModsDownloadJob)change.Current.Definition;
                        UpdateDownloadInfo(_downloadCache.Lookup(change.Current.Id).Value, nexusJob, nexusJob.HttpDownloadJob.Job);
                    }
                    else
                    {
                        var nexusJob = (NexusModsDownloadJob)change.Current.Definition;
                        var info = CreateDownloadInfo(nexusJob, nexusJob.HttpDownloadJob.Job);
                        _downloadCache.AddOrUpdate(info);
                    }
                }
            })
            .DisposeWith(_disposables);
    }
    
    // Observable properties implementation
    public IObservable<IChangeSet<DownloadInfo, JobId>> ActiveDownloads => 
        _downloadCache.Connect()
            .AutoRefresh(x => x.Status)
            .Filter(x => x.Status.IsActive());
    
    public IObservable<IChangeSet<DownloadInfo, JobId>> CompletedDownloads =>
        _downloadCache.Connect()
            .AutoRefresh(x => x.Status)
            .Filter(x => x.Status == JobStatus.Completed);
    
    public IObservable<IChangeSet<DownloadInfo, JobId>> AllDownloads =>
        _downloadCache.Connect();
    
    public IObservable<IChangeSet<DownloadInfo, JobId>> GetDownloadsForGame(GameId gameId) =>
        _downloadCache.Connect()
            .Filter(x => x.GameId.Equals(gameId));
    
    
    // Control operations
    public void PauseDownload(JobId jobId) => _jobMonitor.Pause(jobId);
    public void PauseDownload(DownloadInfo downloadInfo) => _jobMonitor.Pause(downloadInfo.Id);
    
    public void ResumeDownload(JobId jobId) => _jobMonitor.Resume(jobId);
    public void ResumeDownload(DownloadInfo downloadInfo) => _jobMonitor.Resume(downloadInfo.Id);
    
    public void CancelDownload(JobId jobId) => _jobMonitor.Cancel(jobId);
    public void CancelDownload(DownloadInfo downloadInfo) => _jobMonitor.Cancel(downloadInfo.Id);
    public void PauseAll()
    {
        foreach (var download in _downloadCache.Items)
        {
            if (download.Status == JobStatus.Running)
                _jobMonitor.Pause(download.Id);
        }
    }

    public void ResumeAll()
    {
        foreach (var download in _downloadCache.Items)
        {
            if (download.Status == JobStatus.Paused)
                _jobMonitor.Resume(download.Id);
        }
    }
    public void CancelSelected(IEnumerable<JobId> jobIds)
    {
        foreach (var jobId in jobIds)
            _jobMonitor.Cancel(jobId);
    }
    
    private DownloadInfo CreateDownloadInfo(NexusModsDownloadJob nexusJob, IJob httpDownloadJob)
    {
        var info = new DownloadInfo 
        { 
            Id = httpDownloadJob.Id,
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
        info.DownloadUri = httpJobDefinition.Uri;
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
        
        // Find the original job from the job monitor
        var job = _jobMonitor.Jobs.FirstOrDefault(j => j.Id == downloadInfo.Id);
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
