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
        var destination = job.Get(HttpDownloadJobPersistedState.Destination);
        await _downloader.DownloadAsync(
            [job.Get(HttpDownloadJobPersistedState.Uri)],
            destination, 
            token: cancellationToken);
        return JobResult.CreateCompleted(destination);
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
