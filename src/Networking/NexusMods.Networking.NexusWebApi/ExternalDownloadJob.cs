using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Job for external collection downloads.
/// </summary>
public record ExternalDownloadJob : HttpDownloadJob
{
    /// <summary>
    /// The expected MD5 hash value of the downloaded file.
    /// </summary>
    public required Md5HashValue ExpectedMd5 { get; init; }

    /// <summary>
    /// The user-friendly name of the file.
    /// </summary>
    public required string LogicalFileName { get; init; }

    /// <summary>
    /// Create a new download job for the given URL, the job will fail if the downloaded file does not
    /// match the expected MD5 hash.
    /// </summary>
    public static IJobTask<ExternalDownloadJob, AbsolutePath> Create(IServiceProvider provider, Uri uri,
        Md5HashValue expectedMd5, string logicalFileName)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var tempFileManager = provider.GetRequiredService<TemporaryFileManager>();
        var job = new ExternalDownloadJob
        {
            Logger = provider.GetRequiredService<ILogger<ExternalDownloadJob>>(),
            ExpectedMd5 = expectedMd5,
            LogicalFileName = logicalFileName,
            DownloadPageUri = uri,
            Destination = tempFileManager.CreateFile(),
            Uri = uri,
        };

        return monitor.Begin<ExternalDownloadJob, AbsolutePath>(job);
    }


    /// <inheritdoc />
    public override async ValueTask AddMetadata(ITransaction tx, LibraryFile.New libraryFile)
    {
        await using (var fileStream = Destination.Read())
        {
            var algo = MD5.Create();
            var hash = await algo.ComputeHashAsync(fileStream);
            var md5Actual = Md5HashValue.From(hash);
            if (md5Actual != ExpectedMd5)
                throw new InvalidOperationException($"MD5 hash mismatch. Expected: {ExpectedMd5}, Actual: {md5Actual}");
        }

        tx.Add(libraryFile, DirectDownloadLibraryFile.Md5, ExpectedMd5);
        tx.Add(libraryFile, DirectDownloadLibraryFile.LogicalFileName, LogicalFileName);
    }
}
