using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <summary>
/// A worker for downloading files from the internet.
/// </summary>
public class HttpDownloadJobWorker : APersistedJobWorker<HttpDownloadJob>
{
    private readonly IHttpDownloader _downloader;
    private readonly IConnection _connection;

    /// <inheritdoc />
    public override Guid Id { get; } = Guid.Parse("17DBF060-5A55-4960-81D6-F99E4CD24702");

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="downloader"></param>
    public HttpDownloadJobWorker(IConnection connection, IHttpDownloader downloader)
    {
        _connection = connection;
        _downloader = downloader;
    }

    /// <inheritdoc />
    protected override async Task<JobResult> ExecuteAsync(HttpDownloadJob job, CancellationToken cancellationToken)
    {
        await _downloader.DownloadAsync(
            [job.DownloadUri],
            job.DownloadPath, 
            token: cancellationToken);
        return JobResult.CreateCompleted(job.DownloadPath);
    }

    /// <inheritdoc />
    public override IJob LoadJob(PersistedJobState.ReadOnly state)
    {
        return new HttpDownloadJob(_connection, state.Id, null!, worker: this);
    }
    
    /// <summary>
    /// Create a job to download a file from the internet.
    /// </summary>
    public async Task<IJob> CreateJob(Uri uri, AbsolutePath destination)
    {
        return await HttpDownloadJob.Create(_connection, null!, this, uri, destination);
    }
}
