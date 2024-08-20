using Downloader;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Settings;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

public class HttpDownloadJobWorker : APersistedJobWorker<HttpDownloadJob>
{
    private readonly IConnection _connection;
    private readonly IJobMonitor _jobMonitor;
    private readonly ISettingsManager _settingsManager;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpDownloadJobWorker(IConnection connection, IJobMonitor jobMonitor, ISettingsManager settingsManager)
    {
        _connection = connection;
        _jobMonitor = jobMonitor;
        _settingsManager = settingsManager;
        ProgressRateFormatter = new BytesPerSecondFormatter();
    }

    /// <inheritdoc/>
    public override Guid Id { get; } = Guid.Parse("da685bec-d369-4a7c-870c-26edb879f832");

    /// <inheritdoc/>
    protected override async Task<JobResult> ExecuteAsync(HttpDownloadJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!job.DownloadPackage.HasValue)
        {
            job.DownloadPackage = new DownloadPackage
            {
                FileName = job.Destination.ToNativeSeparators(OSInformation.Shared),
                Urls = [job.Uri.ToString()],
            };
        }

        var settings = _settingsManager.Get<HttpDownloaderSettings>();
        var downloadService = new DownloadService(settings.ToConfiguration());
        
        downloadService.DownloadProgressChanged += (sender, args) =>
        {
            // Divide by 100 because the library gives us a value between 0 and 100.
            SetProgress(job, Percent.CreateClamped(args.ProgressPercentage / 100));
            SetProgressRate(job, args.BytesPerSecondSpeed);
        };

        // TODO: progress reporting
        await downloadService.DownloadFileTaskAsync(
            package: job.DownloadPackage.Value,
            cancellationToken: cancellationToken
        );

        return JobResult.CreateCompleted(job.Destination);
    }

    /// <inheritdoc/>
    public override IJob LoadJob(PersistedJobState.ReadOnly state)
    {
        if (!state.TryGetAsHttpDownloadJobPersistedState(out var httpState))
            throw new NotSupportedException();

        return new HttpDownloadJob(
            _connection,
            httpState,
            worker: this,
            monitor: _jobMonitor
        );
    }
}
