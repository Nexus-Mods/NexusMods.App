using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLibraryFileJob : IJobDefinitionWithStart<AddLibraryFileJob, LibraryFile.New>, IAsyncDisposable
{
    public required ITransaction Transaction { get; init; }
    public required AbsolutePath FilePath { get; init; }
    private ConcurrentBag<TemporaryPath> ExtractionDirectories { get; } = [];
    
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

        var toArchive = new List<ArchivedFileEntry>();
        var result = await AnalyzeOne(context, FilePath, toArchive);
        await FileStore.BackupFiles(toArchive, deduplicate: false, context.CancellationToken);
        return result;
    }

    private async Task<LibraryFile.New> AnalyzeOne(IJobContext<AddLibraryFileJob> context, AbsolutePath filePath, List<ArchivedFileEntry> toArchive)
    {
        var isArchive = await CheckIfArchiveAsync(filePath);
        var hash = await filePath.XxHash64Async();
        
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
                var subFile = await AnalyzeOne(context, extracted, toArchive);
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
                toArchive.Add(new ArchivedFileEntry(new NativeFileStreamFactory(filePath), hash, size)); 
        }

        return libraryFile;
    }

    private async Task<bool> CheckIfArchiveAsync(AbsolutePath filePath)
    {
        await using var stream = filePath.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        var canExtract = await FileExtractor.CanExtract(stream);
        return canExtract;
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
