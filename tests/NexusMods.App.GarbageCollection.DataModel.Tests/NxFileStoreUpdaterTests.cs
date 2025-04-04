using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore.Nx.Models;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Archives.Nx.Headers;
using NexusMods.DataModel;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Extensions.Nx.FileProviders;
namespace NexusMods.App.GarbageCollection.DataModel.Tests;

public class NxFileStoreUpdaterTests(IFileStore fileStore, IConnection connection, NxFileStore nxFileStore)
{
    private readonly NxFileStoreUpdater _updater = new(connection);

    [Fact]
    public async Task UpdateNxFileStore_ShouldUpdateArchivedFileEntries()
    {
        // Arrange [Backup the test files]
        var files = new[]
        {
            ("fun.txt", "Down, Down, Down, Left, Right, A"),
            ("is.txt", "FM NO.46"),
            ("infinite.txt", "PCM NO.12"),
            ("with.txt", "DA NO.25"),
            ("Nexus Mods.txt", "Happy Testing!!"),
        };

        // Arrange [Backup the test files], and get their location.
        var (fileNameToHash, archivePath) = await SetupTestFiles(files);

        // Arrange: Perform a repack through the GC.
        //          This repacks without the files 'infinite.txt' and 'with.txt'.
        var collector = SetupGarbageCollector(archivePath, fileNameToHash);
        var newArchivePath = RepackArchive(collector, out var toDelete);

        // Act: Update the offsets in the file store.
        _updater.UpdateNxFileStore(toDelete, archivePath, newArchivePath);

        // Assert
        var streamProvider = new FromAbsolutePathProvider
        {
            FilePath = newArchivePath,
        };
        var header = HeaderParser.ParseHeader(streamProvider);

        foreach (var (fileName, hash) in fileNameToHash)
        {
            var archivedFiles = ArchivedFile.FindByHash(connection.Db, hash).ToList();

            if (fileName is "infinite.txt" or "with.txt")
            {
                var firstFile = archivedFiles.First();
                firstFile.IsValid().Should().BeFalse($"Archived file for {fileName} was retracted. " +
                                                     $"It should not be valid.");
                continue;
            }

            archivedFiles.Should().NotBeEmpty($"Archived file for {fileName} should exist");
            foreach (var archivedFile in archivedFiles)
            {
                archivedFile.NxFileEntry.Should().NotBeNull($"NxFileEntry for {fileName} should not be null");
                archivedFile.NxFileEntry.Hash.Should().Be(hash.Value, $"Hash for {fileName} should match");
                archivedFile.Container.Path.Should().Be(newArchivePath.FileName, "Container path should be updated to new archive");

                // Verify correct offsets
                var headerEntry = header.Entries.FirstOrDefault(e => e.Hash == hash.Value);
                headerEntry.Should().NotBeNull($"Header entry for {fileName} should exist");
                archivedFile.NxFileEntry.Should().BeEquivalentTo(headerEntry, $"NxFileEntry for {fileName} should match header entry");
            }
        }
    }
    
    [Fact]
    public async Task UpdateNxFileStore_WithIncorrectArchivePath_ShouldNotUpdateEntries()
    {
        // This test ensures that UpdateNxFileStore doesn't modify files when given an incorrect archive path.
        // This serves as a security measure against potential issues with duplicate files in FileStore.

        // Arrange
        var files = new[]
        {
            ("konami_0.txt", "Up, Up, Down, Down, Left, Right, Left, Right, B, A, Start"),
            ("contra_1.txt", "30 Lives Activated!"),
        };

        var (fileHashes, correctArchivePath) = await SetupTestFiles(files);

        // Act: Update the offsets in the file store with the incorrect archive path
        //      This should be a no-op; just like when we're working with duplicates
        //      across multiple archives.
        _updater.UpdateNxFileStore(new List<Hash>(), default(AbsolutePath), correctArchivePath);

        // Assert
        foreach (var (fileName, hash) in fileHashes)
        {
            var archivedFiles = ArchivedFile.FindByHash(connection.Db, hash).ToList();

            archivedFiles.Should().NotBeEmpty($"Archived file for {fileName} should still exist");
            foreach (var archivedFile in archivedFiles)
            {
                archivedFile.IsValid().Should().BeTrue($"Archived file for {fileName} should remain valid");
                archivedFile.Container.Path.Should().Be(correctArchivePath.FileName, 
                    "Container path should not be updated when an incorrect archive path is provided");
            }
        }
    }
    
    /// <summary>
    /// Using contents in <param name="files"/>. 
    /// 
    /// Creates a test archive returned as `ArchivePath`.
    /// All items in archive are returned via `FileHashes`.
    /// </summary>
    private async Task<(Dictionary<string, Hash> FileHashes, AbsolutePath ArchivePath)> SetupTestFiles(params (string FileName, string Content)[] files)
    {
        var records = new List<ArchivedFileEntry>();
        var fileHashes = new Dictionary<string, Hash>();

        foreach (var (fileName, content) in files)
        {
            var data = Encoding.UTF8.GetBytes(content);
            var hash = data.AsSpan().xxHash3();
            fileHashes[fileName] = hash;

            var entry = new ArchivedFileEntry(
                new MemoryStreamFactory(RelativePath.FromUnsanitizedInput(fileName), new MemoryStream(data)),
                hash,
                Size.FromLong(data.Length)
            );

            records.Add(entry);
        }

        // Backup the test files and get their location
        await fileStore.BackupFiles(records);
        var archivePath = GetArchivePath(fileHashes.Values.First());

        return (fileHashes, archivePath);
    }

    private AbsolutePath GetArchivePath(Hash hash)
    {
        nxFileStore.TryGetLocation(connection.Db, hash, null, out var archivePath, out _).Should().BeTrue("Archive should exist");
        return archivePath;
    }

    private ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> SetupGarbageCollector(AbsolutePath archivePath, Dictionary<string, Hash> fileHashes)
    {
        var streamProvider = new FromAbsolutePathProvider
        {
            FilePath = archivePath,
        };
        var header = HeaderParser.ParseHeader(streamProvider);

        var collector = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        collector.AddArchive(archivePath, new NxParsedHeaderState(header));
        foreach (var (fileName, hash) in fileHashes)
        {
            if (fileName != "infinite.txt" && fileName != "with.txt")
                collector.AddReferencedFile(hash);
        }

        return collector;
    }

    private AbsolutePath RepackArchive(ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> collector, out List<Hash> toDelete)
    {
        AbsolutePath newArchivePath = default;
        List<Hash> toDel = null!;
        collector.CollectGarbage(new Progress<double>(), (progress, toArchive, toRemove, archive) =>
            {
                toDel = toRemove;
                NxRepacker.RepackArchive(progress, toArchive, toRemove, archive, true, out newArchivePath);
            }
        );

        toDelete = toDel;
        return newArchivePath;
    }
    
    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
