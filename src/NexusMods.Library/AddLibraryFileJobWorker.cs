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

internal class AddLibraryFileJobWorker : AJobWorker<AddLibraryFileJob>
{
    private readonly ILogger _logger;

    private readonly IServiceProvider _serviceProvider;
    private readonly IFileExtractor _fileExtractor;
    private readonly TemporaryFileManager _temporaryFileManager;

    public AddLibraryFileJobWorker(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AddLibraryFileJobWorker>>();

        _serviceProvider = serviceProvider;
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
    }

    protected override async Task<JobResult> ExecuteAsync(AddLibraryFileJob job, CancellationToken cancellationToken)
    {
        var absolutePath = job.FilePath;
        if (!absolutePath.FileExists)
        {
            if (absolutePath.DirectoryExists())
            {
                return JobResult.CreateFailed($"File at `{absolutePath}` can't be added to the library because it's a directory");
            }

            return JobResult.CreateFailed($"File at `{absolutePath}` can't be added to the library because it doesn't exist");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!job.IsArchive.HasValue)
        {
            job.IsArchive = await CheckIfArchiveAsync(job.FilePath);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!job.HashJobResult.HasValue)
        {
            job.HashJobResult = await HashAsync(job.FilePath);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!job.EntityId.HasValue)
        {
            job.EntityId = job.Transaction.TempId();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!job.LibraryFile.HasValue)
        {
            job.LibraryFile = CreateLibraryFile(
                job.Transaction,
                job.EntityId.Value,
                job.FilePath,
                hash: job.HashJobResult.Value.RequireData<Hash>()
            );
        }

        if (job.IsArchive.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!job.LibraryArchive.HasValue)
            {
                job.LibraryArchive = new LibraryArchive.New(job.Transaction, job.EntityId.Value)
                {
                    LibraryFile = job.LibraryFile.Value,
                };
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!job.ExtractionDirectory.HasValue)
            {
                job.ExtractionDirectory = _temporaryFileManager.CreateFolder();
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!job.ExtractedFiles.HasValue)
            {
                job.ExtractedFiles = await ExtractArchiveAsync(
                    job,
                    job.FilePath,
                    job.ExtractionDirectory.Value
                );
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!job.AddExtractedFileJobResults.HasValue)
            {
                job.AddExtractedFileJobResults = await AddJobsAndWaitParallelAsync(job.ExtractedFiles.Value.Select(fileEntry =>
                {
                    var worker = _serviceProvider.GetRequiredService<AddLibraryFileJobWorker>();
                    var job = new AddLibraryFileJob(job, worker)
                    {
                        Transaction = job.Transaction,
                        FilePath = fileEntry.Path,
                        DoCommit = false,
                    };

                    return (AJob)job;
                }).ToArray());
            }

            cancellationToken.ThrowIfCancellationRequested();
            foreach (var jobResult in job.AddExtractedFileJobResults.Value)
            {
                var libraryFile = jobResult.RequireData<LibraryFile.New>();
                var archiveFileEntry = new LibraryArchiveFileEntry.New(job.Transaction, libraryFile.Id)
                {
                    LibraryFile = libraryFile,
                    ParentId = job.LibraryArchive.Value,
                };
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (job.DoCommit)
        {
            var transactionResult = await job.Transaction.Commit();
            return JobResult.CreateCompleted(transactionResult.Remap(job.LibraryFile.Value));
        }

        return JobResult.CreateCompleted(job.LibraryFile.Value);
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

        var worker = JobWorker.Create(hashJob, async static (job, _, cancellationToken) =>
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

    private async Task<IFileEntry[]> ExtractArchiveAsync(
        AJob job,
        AbsolutePath archivePath,
        AbsolutePath outputPath)
    {
        await using var tempDirectory = _temporaryFileManager.CreateFolder();

        var worker = _serviceProvider.GetRequiredService<ExtractArchiveJobWorker>();
        var extractArchiveJob = new ExtractArchiveJob(job, worker)
        {
            FileStreamFactory = new NativeFileStreamFactory(archivePath),
            OutputPath = outputPath,
        };

        await AddJobAndWaitForResultAsync(extractArchiveJob);
        return outputPath.EnumerateFileEntries().ToArray();
    }
}
