using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi;

public class NexusModsDownloadJob : APersistedJob, INXMDownloadJob
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public NexusModsDownloadJob(
        IConnection connection,
        NexusModsDownloadJobPersistedState.ReadOnly state,
        IJobGroup? group = default,
        IJobWorker? worker = default,
        IJobMonitor? monitor = default) : base(connection, state.AsHttpDownloadJobPersistedState().AsPersistedJobState(), null!, group, worker, monitor)
    {
        FileMetadata = state.FileMetadata;
    }

    public required HttpDownloadJob HttpDownloadJob { get; init; }

    public NexusModsFileMetadata.ReadOnly FileMetadata { get; }

    /// <inheritdoc/>
    public AbsolutePath Destination => HttpDownloadJob.Destination;

    /// <inheritdoc/>
    public ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile)
    {
        libraryFile.GetLibraryItem(transaction).Name = FileMetadata.Name;

        _ = new NexusModsLibraryFile.New(transaction, libraryFile.Id)
        {
            FileMetadataId = FileMetadata,
            ModPageMetadataId = FileMetadata.ModPage,
            DownloadedFile = new DownloadedFile.New(transaction, libraryFile.Id)
            {
                DownloadPageUri = HttpDownloadJob.DownloadPageUri,
                LibraryFile = libraryFile,
            },
        };

        return ValueTask.CompletedTask;
    }
}
