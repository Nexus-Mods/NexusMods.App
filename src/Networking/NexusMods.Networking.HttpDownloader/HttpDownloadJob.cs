using System.Net.Http.Headers;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader;

[PublicAPI]
public class HttpDownloadJob : APersistedJob, IHttpDownloadJob
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpDownloadJob(
        IConnection connection,
        HttpDownloadJobPersistedState.ReadOnly state,
        IJobGroup? group = default,
        IJobWorker? worker = default,
        IJobMonitor? monitor = default) : base(connection, state.AsPersistedJobState(), new MutableProgress(new DeterminateProgress(new BytesPerSecondFormatter())), group, worker, monitor
    ) { }

    /// <inheritdoc/>
    public AbsolutePath Destination => Get(HttpDownloadJobPersistedState.Destination);

    /// <inheritdoc/>
    public Uri Uri => Get(HttpDownloadJobPersistedState.Uri);

    /// <inheritdoc/>
    public Uri DownloadPageUri => Get(HttpDownloadJobPersistedState.DownloadPageUri);

    public Size TotalBytesDownloaded { get; set; }
    public Optional<bool> AcceptRanges { get; set; }
    public Optional<Size> ContentLength { get; set; }
    public Optional<EntityTagHeaderValue> ETag { get; set; }

    public bool IsFirstRequest => TotalBytesDownloaded == default(Size);

    /// <inheritdoc/>
    public ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile)
    {
        return ValueTask.CompletedTask;
    }
}
