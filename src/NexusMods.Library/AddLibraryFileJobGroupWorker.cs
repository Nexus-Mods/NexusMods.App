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

internal class AddLibraryFileJobGroupWorker : AJobGroupWorker<AddLibraryFileJobGroup>
{
    private readonly ILogger _logger;

    private readonly IServiceProvider _serviceProvider;
    private readonly IFileExtractor _fileExtractor;
    private readonly TemporaryFileManager _temporaryFileManager;

    public AddLibraryFileJobGroupWorker(
        IServiceProvider serviceProvider,
        AddLibraryFileJobGroup job) : base(job)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AddLibraryFileJobGroupWorker>>();

        _serviceProvider = serviceProvider;
        _fileExtractor = serviceProvider.GetRequiredService<IFileExtractor>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
    }

    protected override async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var absolutePath = JobGroup.FilePath;
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
            if (!JobGroup.LibraryArchive.HasValue)
            {
                JobGroup.LibraryArchive = new LibraryArchive.New(JobGroup.Transaction, JobGroup.EntityId.Value)
                {
                    LibraryFile = JobGroup.LibraryFile.Value,
                };
            }

            if (!JobGroup.ExtractionDirectory.HasValue)
            {
                JobGroup.ExtractionDirectory = _temporaryFileManager.CreateFolder();
            }

            if (!JobGroup.ExtractedFiles.HasValue)
            {
                JobGroup.ExtractedFiles = await ExtractArchiveAsync(
                    JobGroup.FilePath,
                    JobGroup.ExtractionDirectory.Value
                );
            }

            if (!JobGroup.AddExtractedFileJobResults.HasValue)
            {
                JobGroup.AddExtractedFileJobResults = await AddJobsAndWaitParallelAsync(JobGroup.ExtractedFiles.Value.Select(fileEntry =>
                {
                    var worker = _serviceProvider.GetRequiredService<AddLibraryFileJobGroupWorker>();
                    var job = new AddLibraryFileJobGroup(JobGroup, worker)
                    {
                        Transaction = JobGroup.Transaction,
                        FilePath = fileEntry.Path,
                        DoCommit = false,
                    };

                    return (AJob)job;
                }).ToArray());
            }

            foreach (var jobResult in JobGroup.AddExtractedFileJobResults.Value)
            {
                var libraryFile = RequireDataFromResult<LibraryFile.New>(jobResult);
                var archiveFileEntry = new LibraryArchiveFileEntry.New(JobGroup.Transaction, libraryFile.Id)
                {
                    LibraryFile = libraryFile,
                    ParentId = JobGroup.LibraryArchive.Value,
                };
            }
        }

        if (JobGroup.DoCommit)
        {
            var transactionResult = await JobGroup.Transaction.Commit();
            return CompleteJob(transactionResult.Remap(JobGroup.LibraryFile.Value));
        }

        return CompleteJob(JobGroup.LibraryFile.Value);
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

    private async Task<IFileEntry[]> ExtractArchiveAsync(
        AbsolutePath archivePath,
        AbsolutePath outputPath)
    {
        await using var tempDirectory = _temporaryFileManager.CreateFolder();

        var worker = _serviceProvider.GetRequiredService<ExtractArchiveJobGroupWorker>();
        var extractArchiveJob = new ExtractArchiveJobGroup(JobGroup, worker)
        {
            FileStreamFactory = new NativeFileStreamFactory(archivePath),
            OutputPath = outputPath,
        };

        await AddJobAndWaitForResultAsync(extractArchiveJob);
        return outputPath.EnumerateFileEntries().ToArray();
    }
}
