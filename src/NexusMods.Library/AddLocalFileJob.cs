using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
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

        Md5HashValue md5;
        await using (var fileStream = FilePath.Read())
        {
            using var algo = MD5.Create();
            var rawHash = await algo.ComputeHashAsync(fileStream, cancellationToken: context.CancellationToken);
            md5 = Md5HashValue.From(rawHash);
        }

        Debug.Assert(!md5.Equals(default(Md5HashValue)));
        tx.Add(libraryFile.LibraryFileId, LibraryFile.Md5, md5);

        var localFile = new LocalFile.New(tx, libraryFile.LibraryFileId)
        {
            LibraryFile = libraryFile,
            OriginalPath = FilePath.ToString(),
        };

        var transactionResult = await tx.Commit();
        return transactionResult.Remap(localFile);
    }
}
