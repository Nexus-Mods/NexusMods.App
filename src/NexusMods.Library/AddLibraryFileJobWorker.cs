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

    public AddLibraryFileJobWorker(
        IServiceProvider serviceProvider,
        AddLibraryFileJob job) : base(job)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AddLibraryFileJobWorker>>();

        _serviceProvider = serviceProvider;
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
    }

    protected override async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var absolutePath = Job.FilePath;
        if (!absolutePath.FileExists)
        {
            if (absolutePath.DirectoryExists())
            {
                return FailJob($"File at `{absolutePath}` can't be added to the library because it's a directory");
            }

            return FailJob($"File at `{absolutePath}` can't be added to the library because it doesn't exist");
        }

        ThrowIsPausedOrCancelled(cancellationToken);
        if (!Job.IsArchive.HasValue)
        {
            Job.IsArchive = await CheckIfArchiveAsync(Job.FilePath);
        }

        if (!Job.HashJobResult.HasValue)
        {
            Job.HashJobResult = await HashAsync(Job.FilePath);
        }

        if (!Job.EntityId.HasValue)
        {
            Job.EntityId = Job.Transaction.TempId();
        }

        if (!Job.LibraryFile.HasValue)
        {
            Job.LibraryFile = CreateLibraryFile(
                Job.Transaction,
                Job.EntityId.Value,
                Job.FilePath,
                hash: RequireDataFromResult<Hash>(Job.HashJobResult.Value)
            );
        }

        if (Job.IsArchive.Value)
        {
            if (!Job.LibraryArchive.HasValue)
            {
                Job.LibraryArchive = new LibraryArchive.New(Job.Transaction, Job.EntityId.Value)
                {
                    LibraryFile = Job.LibraryFile.Value,
                };
            }

            if (!Job.ExtractionDirectory.HasValue)
            {
                Job.ExtractionDirectory = _temporaryFileManager.CreateFolder();
            }

            if (!Job.ExtractedFiles.HasValue)
            {
                Job.ExtractedFiles = await ExtractArchiveAsync(
                    Job.FilePath,
                    Job.ExtractionDirectory.Value
                );
            }

            if (!Job.AddExtractedFileJobResults.HasValue)
            {
                Job.AddExtractedFileJobResults = await AddJobsAndWaitParallelAsync(Job.ExtractedFiles.Value.Select(fileEntry =>
                {
                    var worker = _serviceProvider.GetRequiredService<AddLibraryFileJobWorker>();
                    var job = new AddLibraryFileJob(Job, worker)
                    {
                        Transaction = Job.Transaction,
                        FilePath = fileEntry.Path,
                        DoCommit = false,
                    };

                    return (AJob)job;
                }).ToArray());
            }

            foreach (var jobResult in Job.AddExtractedFileJobResults.Value)
            {
                var libraryFile = RequireDataFromResult<LibraryFile.New>(jobResult);
                var archiveFileEntry = new LibraryArchiveFileEntry.New(Job.Transaction, libraryFile.Id)
                {
                    LibraryFile = libraryFile,
                    ParentId = Job.LibraryArchive.Value,
                };
            }
        }

        if (Job.DoCommit)
        {
            var transactionResult = await Job.Transaction.Commit();
            return CompleteJob(transactionResult.Remap(Job.LibraryFile.Value));
        }

        return CompleteJob(Job.LibraryFile.Value);
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

        // Worker.Create(hashJob, async static (job, controller, cancellationToken) =>
        // {
        //     var stream = job.Stream;
        //     stream.Position = 0;
        //
        //     var hash = await stream.XxHash64Async(token: cancellationToken);
        //     stream.Position = 0;
        //
        //     return hash;
        // });

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

    private async Task<IFileEntry[]> ExtractArchiveAsync(
        AbsolutePath archivePath,
        AbsolutePath outputPath)
    {
        await using var tempDirectory = _temporaryFileManager.CreateFolder();

        var worker = _serviceProvider.GetRequiredService<ExtractArchiveJobWorker>();
        var extractArchiveJob = new ExtractArchiveJob(Job, worker)
        {
            FileStreamFactory = new NativeFileStreamFactory(archivePath),
            OutputPath = outputPath,
        };

        await AddJobAndWaitForResultAsync(extractArchiveJob);
        return outputPath.EnumerateFileEntries().ToArray();
    }
}
