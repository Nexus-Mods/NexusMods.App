using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="IDownloadsService"/>.
/// </summary>
public sealed class DownloadsService : IDownloadsService, IDisposable
{
    private readonly IJobMonitor _jobMonitor;
    // private readonly IGameRegistry _gameRegistry;
    private readonly ILibraryService _libraryService;
    private readonly IConnection _connection;
    private readonly CompositeDisposable _disposables = new();
    
    private readonly SourceCache<DownloadInfo, JobId> _downloadCache = new(x => x.Id);
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public DownloadsService(
        IJobMonitor jobMonitor,
        ILibraryService libraryService,
        IConnection connection)
    {
        _jobMonitor = jobMonitor;
        _libraryService = libraryService;
        _connection = connection;
        
        InitializeObservables();
    }
    
    private void InitializeObservables()
    {
        // Monitor all download jobs and transform them into DownloadInfo
        _jobMonitor.GetObservableChangeSet<IHttpDownloadJob>()
            .Transform(CreateDownloadInfo)
            .PopulateInto(_downloadCache)
            .DisposeWith(_disposables);
            
        // Update download info with a 1 second poll.
        _jobMonitor.GetObservableChangeSet<IHttpDownloadJob>()
            .AutoRefreshOnObservable(_ => Observable.Interval(TimeSpan.FromSeconds(1)))
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    if (change.Reason is not (ChangeReason.Refresh or ChangeReason.Update))
                        continue;

                    if (_downloadCache.Lookup(change.Current.Id).HasValue)
                        UpdateDownloadInfo(_downloadCache.Lookup(change.Current.Id).Value, change.Current);
                    else
                    {
                        var info = CreateDownloadInfo(change.Current);
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
            .Filter(x => x.GameId.HasValue && x.GameId.Value.Equals(gameId));
    
    public IObservable<IReadOnlyDictionary<GameId, int>> DownloadCountsByGame =>
        _downloadCache.Connect()
            .Filter(x => x.GameId.HasValue)
            .QueryWhenChanged(items =>
            {
                // Note(sewer): This is slow because it re-evals on every change.
                //              Will fixup depending on when/where it's used.
                return items.Items
                    .Where(x => x.GameId.HasValue)
                    .GroupBy(x => x.GameId.Value)
                    .ToDictionary(g => g.Key, g => g.Count()) as IReadOnlyDictionary<GameId, int>;
            });
    
    public IObservable<Size> AggregateDownloadSpeed =>
        ActiveDownloads
            .QueryWhenChanged(items =>
            {
                var totalBytesPerSecond = items.Items.Sum(x => (long)x.TransferRate.Value);
                return Size.FromLong(totalBytesPerSecond);
            });
    
    // Control operations
    public void PauseDownload(JobId jobId) => _jobMonitor.Pause(jobId);
    public void ResumeDownload(JobId jobId) => _jobMonitor.Resume(jobId);
    public void CancelDownload(JobId jobId) => _jobMonitor.Cancel(jobId);
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
    
    private DownloadInfo CreateDownloadInfo(IJob job)
    {
        var info = new DownloadInfo { Id = job.Id };
        PopulateDownloadInfo(info, job);
        info.StartedAt = DateTimeOffset.UtcNow; // TODO: Track actual start time
        return info;
    }
    
    private void UpdateDownloadInfo(DownloadInfo info, IJob job) => PopulateDownloadInfo(info, job);

    private void PopulateDownloadInfo(DownloadInfo info, IJob job)
    {
        var httpJob = job.Definition as IHttpDownloadJob;
        
        info.Name = ExtractName(httpJob);
        info.GameId = ExtractGameId(httpJob);
        info.FileSize = GetFileSize(job);
        info.Progress = job.Progress.HasValue ? job.Progress.Value : Percent.Zero;
        info.DownloadedBytes = GetDownloadedBytes(job);
        info.EstimatedTimeRemaining = CalculateEstimatedTime(job);
        info.TransferRate = Size.FromLong((long)(job.RateOfProgress.HasValue ? job.RateOfProgress.Value : 0));
        info.Status = job.Status;
        info.DownloadUri = httpJob?.Uri != null ? Optional<Uri>.Create(httpJob.Uri) : Optional<Uri>.None;
        info.DownloadPageUri = httpJob?.DownloadPageUri != null ? Optional<Uri>.Create(httpJob.DownloadPageUri) : Optional<Uri>.None;
        info.CompletedFile = GetCompletedFile(job);
        info.CompletedAt = job.Status == JobStatus.Completed ? DateTimeOffset.UtcNow : Optional<DateTimeOffset>.None;
    }
    
    // Helper methods
    private Optional<GameId> ExtractGameId(IHttpDownloadJob? job)
    {
        // TODO: Implement logic to extract game ID from download metadata
        return Optional<GameId>.None;
    }
    
    private string ExtractName(IHttpDownloadJob? job)
    {
        // TODO: Implement logic to extract name from download metadata
        if (job == null) return "Unknown Download";
        
        // Try to extract name from URI or use fallback
        if (job.Destination != default(AbsolutePath))
            return job.Destination.FileName;
        
        return job.Uri?.Segments.LastOrDefault() ?? "Unknown Download";
    }
    
    private Size GetFileSize(IJob job)
    {
        // TODO: Get actual file size from HTTP headers or job metadata
        return Size.Zero;
    }
    
    private Size GetDownloadedBytes(IJob job)
    {
        // TODO: Get actual downloaded bytes from job metadata
        return Size.Zero;
    }
    
    private Optional<TimeSpan> CalculateEstimatedTime(IJob job)
    {
        if (!job.Progress.HasValue || !job.RateOfProgress.HasValue || job.RateOfProgress.Value <= 0)
            return Optional<TimeSpan>.None;
        
        var remainingPercent = 1.0 - job.Progress.Value.Value;
        var totalSize = GetFileSize(job);
        var remainingBytes = totalSize.Value * remainingPercent;
        var secondsRemaining = remainingBytes / job.RateOfProgress.Value;
        
        return Optional<TimeSpan>.Create(TimeSpan.FromSeconds(secondsRemaining));
    }
    
    private Optional<LibraryFile.ReadOnly> GetCompletedFile(IJob job)
    {
        // TODO: Get completed file from job result.
        return Optional<LibraryFile.ReadOnly>.None;
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
        _downloadCache.Dispose();
    }
}
