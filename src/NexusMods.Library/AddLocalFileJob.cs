using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Games.FileHashes.HashValues;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLocalFileJob : IJobDefinitionWithStart<AddLocalFileJob, LocalFile.ReadOnly>, IAddLocalFile
{ 
    public required AbsolutePath FilePath { get; init; }
    internal required IConnection Connection { get; init; }
    internal required IServiceProvider ServiceProvider { get; set; }
    
    public static IJobTask<AddLocalFileJob, LocalFile.ReadOnly> Create(IServiceProvider provider, AbsolutePath filePath)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new AddLocalFileJob
        {
            FilePath = filePath,
            Connection = provider.GetRequiredService<IConnection>(),
            ServiceProvider = provider,
        };
        return monitor.Begin<AddLocalFileJob, LocalFile.ReadOnly>(job);
    }

    public async ValueTask<LocalFile.ReadOnly> StartAsync(IJobContext<AddLocalFileJob> context)
    {
        using var tx = Connection.BeginTransaction();

        var libraryFile = await AddLibraryFileJob.Create(ServiceProvider, tx, FilePath, doCommit: true, doBackup: false);

        // TODO: for perf, read the file once and compute both hashes
        Md5Hash md5HashValue;
        await using (var fileStream = FilePath.Read())
        {
            var algo = MD5.Create();
            var hash = await algo.ComputeHashAsync(fileStream);
            md5HashValue = Md5Hash.From(hash);
        }

        Debug.Assert(!md5HashValue.Equals(default(Md5Hash)));
        var localFile = new LocalFile.New(tx, libraryFile.LibraryFileId)
        {
            LibraryFile = libraryFile,
            OriginalPath = FilePath.ToString(),
            Md5 = md5HashValue,
        };

        var transactionResult = await tx.Commit();
        return transactionResult.Remap(localFile);
    }
}
