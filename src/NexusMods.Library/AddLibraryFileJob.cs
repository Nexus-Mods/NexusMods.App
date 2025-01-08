using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Library;

internal class AddLibraryFileJob : IJobDefinitionWithStart<AddLibraryFileJob, LibraryFile.New>, IAsyncDisposable
{
    public required ITransaction Transaction { get; init; }
    public required AbsolutePath FilePath { get; init; }
    private ConcurrentBag<TemporaryPath> ExtractionDirectories { get; } = [];
    private ConcurrentBag<ArchivedFileEntry> ToArchive { get; } = [];

    private Extension[] NestedArchiveExtensions { get; } =
    [
        KnownExtensions._7z, KnownExtensions.Rar, KnownExtensions.Zip, KnownExtensions._7zip,
        // Stardew Valley SMAPI nested archive
        new(".dat"),
    ];

    public static IJobTask<AddLibraryFileJob, LibraryFile.New> Create(IServiceProvider provider, ITransaction transaction, AbsolutePath filePath, bool doCommit, bool doBackup)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new AddLibraryFileJob
        {
            Transaction = transaction,
            FilePath = filePath,
            FileExtractor = provider.GetRequiredService<IFileExtractor>(),
            Connection = provider.GetRequiredService<IConnection>(),
            TemporaryFileManager = provider.GetRequiredService<TemporaryFileManager>(),
            FileStore = provider.GetRequiredService<IFileStore>(),
        };
        return monitor.Begin<AddLibraryFileJob, LibraryFile.New>(job);
    }

    internal required IFileStore FileStore { get; set; }
    internal required TemporaryFileManager TemporaryFileManager { get; init; }
    internal required IFileExtractor FileExtractor { get; init; }
    public required IConnection Connection { get; set; }

    public async ValueTask<LibraryFile.New> StartAsync(IJobContext<AddLibraryFileJob> context)
    {
        if (!FilePath.FileExists)
            throw new Exception($"File '{FilePath}' does not exist.");

        var topFile = await AnalyzeFile(context, FilePath);
        await FileStore.BackupFiles(ToArchive, deduplicate: false, context.CancellationToken);
        return topFile;
    }

    private async Task<LibraryFile.New> AnalyzeFile(IJobContext<AddLibraryFileJob> context, AbsolutePath filePath, bool isNestedFile = false)
    {
        var isArchive = isNestedFile ? await CheckIfNestedArchiveAsync(filePath) : await CheckIfArchiveAsync(filePath);
        var hash = await filePath.XxHash3Async();

        var libraryFile = CreateLibraryFile(Transaction, filePath, hash);

        if (isArchive)
        {
            var libraryArchive = new LibraryArchive.New(Transaction, libraryFile.Id)
            {
                IsArchive = true,
                LibraryFile = libraryFile,
            };

            var extractionFolder = TemporaryFileManager.CreateFolder();
            // Add the temp folder for later
            ExtractionDirectories.Add(extractionFolder);
            await FileExtractor.ExtractAllAsync(filePath, extractionFolder);

            var extractedFiles = extractionFolder.Path.EnumerateFiles();

            foreach (var extracted in extractedFiles)
            {
                var subFile = await AnalyzeFile(context, extracted, isNestedFile: true);
                var path = extracted.RelativeTo(extractionFolder.Path);
                _ = new LibraryArchiveFileEntry.New(Transaction, subFile.Id)
                {
                    Path = path,
                    LibraryFile = subFile,
                    ParentId = libraryArchive.Id,
                };
            }
        }
        else
        {
            var size = filePath.FileInfo.Size;
            if (!await FileStore.HaveFile(hash))
                ToArchive.Add(new ArchivedFileEntry(new NativeFileStreamFactory(filePath), hash, size));
        }

        return libraryFile;
    }


    /// <summary>
    /// Returns true if the file can be considered an archive and extracted.
    /// </summary>
    private async Task<bool> CheckIfArchiveAsync(AbsolutePath filePath)
    {
        await using var stream = filePath.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        var canExtract = await FileExtractor.CanExtract(stream);
        return canExtract;
    }

    /// <summary>
    /// Returns true if a file that is nested inside an archive can/should be extracted.
    /// This check is more restrictive than just <see cref="CheckIfArchiveAsync"/>, with a filter of supported extensions.
    /// Some games support archive formats as valid mod files, those should not be extracted but treated as a single file instead.
    /// </summary>
    private async Task<bool> CheckIfNestedArchiveAsync(AbsolutePath filePath)
    {
        if (NestedArchiveExtensions.Contains(filePath.Extension))
            return await CheckIfArchiveAsync(filePath);
        return false;
    }

    private static LibraryFile.New CreateLibraryFile(ITransaction tx, AbsolutePath filePath, Hash hash)
    {
        var libraryFile = new LibraryFile.New(tx, out var id)
        {
            FileName = filePath.FileName,
            Hash = hash,
            Size = filePath.FileInfo.Size,
            LibraryItem = new LibraryItem.New(tx, id)
            {
                Name = filePath.FileName,
            },
        };

        return libraryFile;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var directory in ExtractionDirectories)
        {
            await directory.DisposeAsync();
        }

        ExtractionDirectories.Clear();
    }
}
