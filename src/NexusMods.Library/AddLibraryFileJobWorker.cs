using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

[UsedImplicitly]
internal class AddLibraryFileJobWorker : AJobWorker<AddLibraryFileJob>
{
    private readonly ILogger _logger;

    private readonly IServiceProvider _serviceProvider;
    private readonly IFileExtractor _fileExtractor;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IFileStore _fileStore;

    public AddLibraryFileJobWorker(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AddLibraryFileJobWorker>>();

        _serviceProvider = serviceProvider;
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
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
            job.HashJobResult = await HashAsync(job.FilePath, cancellationToken);
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
            if (!job.ExtractionDirectory.HasValue)
            {
                job.ExtractionDirectory = _temporaryFileManager.CreateFolder();
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!job.ExtractedFiles.HasValue)
            {
                var extractedFiles = await ExtractArchiveAsync(
                    job,
                    job.FilePath,
                    job.ExtractionDirectory.Value,
                    cancellationToken
                );

                if (extractedFiles.Length == 0)
                {
                    _logger.LogWarning("File `{Path}` was assumed to be extractable but no files were extracted, it will not be added as an archive", job.FilePath);
                    job.IsArchive = false;
                }
                else
                {
                    job.ExtractedFiles = extractedFiles;
                }
            }
        }

        if (job.IsArchive.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!job.LibraryArchive.HasValue)
            {
                job.LibraryArchive = new LibraryArchive.New(job.Transaction, job.EntityId.Value)
                {
                    LibraryFile = job.LibraryFile.Value,
                    IsIsLibraryArchiveMarker = true,
                };
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!job.AddExtractedFileJobResults.HasValue)
            {
                var extractedFiles = job.ExtractedFiles.Value;
                var results = new ValueTuple<JobResult, IFileEntry>[extractedFiles.Length];

                await Parallel.ForAsync(fromInclusive: 0, toExclusive: extractedFiles.Length, cancellationToken, async (i, innerCancellationToken) =>
                {
                    var fileEntry = extractedFiles[i];

                    var worker = _serviceProvider.GetRequiredService<AddLibraryFileJobWorker>();
                    await using var childJob = new AddLibraryFileJob(job, worker)
                    {
                        Transaction = job.Transaction,
                        FilePath = fileEntry.Path,
                        DoCommit = false,
                        DoBackup = false,
                    };

                    await worker.StartAsync(childJob, cancellationToken: innerCancellationToken);
                    var result = await childJob.WaitToFinishAsync(cancellationToken: innerCancellationToken);

                    results[i] = (result, fileEntry);
                });

                job.AddExtractedFileJobResults = results;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!job.HasBackup.HasValue)
            {
                var filesToBackup = job.AddExtractedFileJobResults.Value
                    .Select(tuple =>
                    {
                        var (jobResult, fileEntry) = tuple;
                        var data = jobResult.RequireData<LibraryFile.New>();

                        return new ArchivedFileEntry
                        {
                            Hash = data.Hash,
                            Size = data.Size,
                            StreamFactory = new NativeFileStreamFactory(fileEntry.Path),
                        };
                    })
                    .ToArray();

                await _fileStore.BackupFiles(filesToBackup, token: cancellationToken);
                job.HasBackup = true;
            }

            cancellationToken.ThrowIfCancellationRequested();
            foreach (var tuple in job.AddExtractedFileJobResults.Value)
            {
                var (jobResult, fileEntry) = tuple;
                var libraryFile = jobResult.RequireData<LibraryFile.New>();
                var path = fileEntry.Path.RelativeTo(job.ExtractionDirectory.Value.Path);

                _ = new LibraryArchiveFileEntry.New(job.Transaction, libraryFile.Id)
                {
                    Path = path,
                    LibraryFile = libraryFile,
                    ParentId = job.LibraryArchive.Value,
                };
            }
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (job is { DoBackup: true, HasBackup.HasValue: false })
            {
                var archivedFileEntry = new ArchivedFileEntry
                {
                    Hash = job.HashJobResult.Value.RequireData<Hash>(),
                    Size = job.FilePath.FileInfo.Size,
                    StreamFactory = new NativeFileStreamFactory(job.FilePath),
                };

                await _fileStore.BackupFiles([archivedFileEntry], token: cancellationToken);
                job.HasBackup = true;
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
        var canExtract = await _fileExtractor.CanExtract(stream);
        return canExtract;
    }

    private static async Task<JobResult> HashAsync(AbsolutePath filePath, CancellationToken cancellationToken)
    {
        await using var stream = filePath.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        stream.Position = 0;

        var hash = await stream.XxHash64Async(token: cancellationToken);
        stream.Position = 0;

        return JobResult.CreateCompleted(hash);

        // TODO:
        // var hashJob = new HashJob
        // {
        //     Stream = filePath.Open(FileMode.Open, FileAccess.Read, FileShare.None),
        // };
        //
        // var worker = JobWorker.Create(hashJob, async static (job, _, cancellationToken) =>
        // {
        //     var stream = job.Stream;
        //     stream.Position = 0;
        //
        //     var hash = await stream.XxHash64Async(token: cancellationToken);
        //     stream.Position = 0;
        //
        //     return hash;
        // });
        //
        // return await AddJobAndWaitForResultAsync(hashJob);
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
        AbsolutePath outputPath,
        CancellationToken cancellationToken)
    {
        var worker = _serviceProvider.GetRequiredService<ExtractArchiveJobWorker>();
        var fileStreamFactory = new NativeFileStreamFactory(archivePath);

        await using var extractArchiveJob = new ExtractArchiveJob(job, worker)
        {
            FileStreamFactory = fileStreamFactory,
            OutputPath = outputPath,
        };

        await worker.StartAsync(extractArchiveJob, cancellationToken: cancellationToken);
        await extractArchiveJob.WaitToFinishAsync(cancellationToken: cancellationToken);
        return outputPath.EnumerateFileEntries().ToArray();
    }
}
