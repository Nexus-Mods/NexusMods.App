using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLocalFileJobGroupWorker : AJobGroupWorker<AddLocalFileJobGroup>
{
    private readonly ILogger _logger;

    private readonly IServiceProvider _serviceProvider;
    private readonly IFileExtractor _fileExtractor;
    private readonly TemporaryFileManager _temporaryFileManager;

    public AddLocalFileJobGroupWorker(
        IServiceProvider serviceProvider,
        AddLocalFileJobGroup job) : base(job)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AddLocalFileJobGroup>>();

        _serviceProvider = serviceProvider;
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
    }

    protected override async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var absolutePath = JobGroup.FilePath;
        _logger.LogInformation("Adding local file at `{Path}` to the library", absolutePath);

        if (!absolutePath.FileExists)
        {
            if (absolutePath.DirectoryExists())
            {
                return FailJob($"File at `{absolutePath}` can't be added to the library because it's a directory");
            }

            return FailJob($"File at `{absolutePath}` can't be added to the library because it doesn't exist");
        }

        if (!JobGroup.IsArchive.HasValue)
        {
            JobGroup.IsArchive = await CheckIfArchiveAsync(JobGroup.FilePath);
        }

        if (!JobGroup.HashJobResult.HasValue)
        {
            JobGroup.HashJobResult = await HashAsync(JobGroup.FilePath);
        }

        if (!JobGroup.EntityId.HasValue)
        {
            JobGroup.EntityId = JobGroup.Transaction.TempId();
        }

        if (!JobGroup.LibraryFile.HasValue)
        {
            JobGroup.LibraryFile = CreateLibraryFile(
                JobGroup.Transaction,
                JobGroup.EntityId.Value,
                JobGroup.FilePath,
                hash: RequireDataFromResult<Hash>(JobGroup.HashJobResult.Value)
            );
        }

        if (JobGroup.IsArchive.Value)
        {

        }

        var localFile = new LocalFile.New(JobGroup.Transaction, JobGroup.EntityId.Value)
        {
            LibraryFile = JobGroup.LibraryFile.Value,
            OriginalPath = absolutePath.GetFullPath(),
        };

        var transactionResult = await JobGroup.Transaction.Commit();
        return CompleteJob(transactionResult.Remap(localFile));
    }

    private async Task<bool> CheckIfArchiveAsync(AbsolutePath filePath)
    {
        await using var stream = filePath.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        return await _fileExtractor.CanExtract(stream);
    }

    private async Task<JobResult> HashAsync(AbsolutePath filePath)
    {
        var hashJob = new HashJob
        {
            Stream = filePath.Open(FileMode.Open, FileAccess.Read, FileShare.None),
        };

        Worker.AddFromStaticFunction(hashJob, async static (job, cancellationToken) =>
        {
            var stream = job.Stream;
            stream.Position = 0;

            var hash = await stream.XxHash64Async(token: cancellationToken);
            stream.Position = 0;

            return hash;
        });

        return await AddJobAndWaitForResultAsync(hashJob);
    }

    private static LibraryFile.New CreateLibraryFile(ITransaction tx, EntityId entityId, AbsolutePath filePath, Hash hash)
    {
        var libraryFile = new LibraryFile.New(tx, entityId)
        {
            FileName = filePath.FileName,
            Hash = hash,
            Size = filePath.FileInfo.Size,
            LibraryItem = new LibraryItem.New(tx, entityId)
            {
                Name = filePath.FileName,
            },
        };

        return libraryFile;
    }

    private async Task ExtractArchiveAsync(
        ITransaction tx,
        LibraryArchive.New libraryArchive,
        AbsolutePath archivePath,
        CancellationToken cancellationToken)
    {
        await using var tempDirectory = _temporaryFileManager.CreateFolder();

        var worker = _serviceProvider.GetRequiredService<ExtractArchiveJobGroupWorker>();
        var extractArchiveJob = new ExtractArchiveJobGroup(JobGroup, worker)
        {
            FileStreamFactory = new NativeFileStreamFactory(archivePath),
            OutputPath = tempDirectory,
        };

        await AddJobAndWaitForResultAsync(extractArchiveJob);

        var files = tempDirectory.Path.EnumerateFileEntries().ToArray();

        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            CancellationToken = cancellationToken,
        }, async (file, innerCancellationToken) =>
        {
            var libraryFile = await AddLibraryFileAsync(tx, tx.TempId(), file.Path, innerCancellationToken);
            var archiveFileEntry = new LibraryArchiveFileEntry.New(tx, libraryFile.Id)
            {
                LibraryFile = libraryFile,
                ParentId = libraryArchive.Id,
            };
        });
    }
}
