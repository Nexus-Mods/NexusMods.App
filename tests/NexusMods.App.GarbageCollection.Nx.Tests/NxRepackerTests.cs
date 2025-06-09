using FluentAssertions;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Paths.Extensions.Nx.Extensions;
using NexusMods.Paths.Extensions.Nx.FileProviders;
using NexusMods.Paths.TestingHelpers;
using Xunit;
namespace NexusMods.App.GarbageCollection.Nx.Tests;

public class NxRepackerTests
{
    private const string File1Name = "file1.txt";
    private const string File2Name = "file2.txt";
    private const string File3Name = "file3.txt";
    private const string File1Content = "Content 1";
    private const string File2Content = "Content 2";
    private const string File3Content = "Content 3";
    private const string OriginalArchiveName = "archive.nx";

    [Theory, AutoFileSystem]
    public async Task CollectGarbage_ShouldRepackArchiveCorrectly(InMemoryFileSystem fs, AbsolutePath folderPath)
    {
/*
    This test verifies that the NxRepacker correctly repacks an Nx archive
    during the garbage collection process.
    
    Specifically:

    1. We start with an archive containing three files: file1.txt, file2.txt, and file3.txt.
    2. We set up the ArchiveGarbageCollector to consider file1.txt and file2.txt as "referenced"
       (i.e., still in use), while file3.txt is not referenced. This emulates a scenario where
       a loadout may have been removed.
       
    3. We expect the NxRepacker to create a new archive that:
       a) Contains only the referenced files (file1.txt and file2.txt)
       b) Does not contain the unreferenced file (file3.txt)
       c) Preserves the content of the referenced files
*/

        // Arrange
        var archivePath = await CreateInitialArchive(fs, folderPath);
        var collector = SetupGarbageCollector(archivePath, out var header);
        AddFile1AndFile2(header, collector);

        // Act
        collector.CollectGarbage(new Progress<double>(), (progress, toArchive, toRemove, archiveRef) =>
        {
            // We create a new archive, so we need its location.
            NxRepacker.RepackArchive(progress, toArchive, toRemove, archiveRef, true, out archivePath);
        });

        // Assert
        var unpacker = NxUnpackerBuilderExtensions.FromFile(archivePath);
        var entries = unpacker.GetPathedFileEntries();

        entries.Should().HaveCount(2);
        entries.Should().Contain(e => e.FilePath == File1Name);
        entries.Should().Contain(e => e.FilePath == File2Name);
        entries.Should().NotContain(e => e.FilePath == File3Name);

        var extractFolder = folderPath.Parent.Combine("extracted");
        unpacker.AddAllFilesWithFileSystemOutput(extractFolder).Extract();

        var extractedFile1 = extractFolder.Combine(File1Name);
        var extractedFile2 = extractFolder.Combine(File2Name);
        var extractedFile3 = extractFolder.Combine(File3Name);

        fs.FileExists(extractedFile1).Should().BeTrue();
        fs.FileExists(extractedFile2).Should().BeTrue();
        fs.FileExists(extractedFile3).Should().BeFalse();

        (await fs.ReadAllTextAsync(extractedFile1)).Should().Be(File1Content);
        (await fs.ReadAllTextAsync(extractedFile2)).Should().Be(File2Content);
    }
    
    [Theory, AutoFileSystem]
    public async Task CollectGarbage_ShouldCreateEmptyArchiveWhenAllFilesUnreferenced(InMemoryFileSystem fs, AbsolutePath folderPath)
    {
/*
    This test verifies that the NxRepacker produces no new files when there are no
    files to be repacked. i.e. It does not produce empty archives.
*/
        
        // Arrange
        var initialArchivePath = await CreateInitialArchive(fs, folderPath);
        var collector = SetupGarbageCollector(initialArchivePath, out _);
        initialArchivePath.FileExists.Should().BeTrue();

        AbsolutePath newArchivePath = default;

        // Act
        collector.CollectGarbage(new Progress<double>(), (progress, toArchive, toRemove, archiveRef) =>
        {
            NxRepacker.RepackArchive(progress, toArchive, toRemove, archiveRef, deleteOriginal: true, out newArchivePath);
        });

        // Assert
        initialArchivePath.FileExists.Should().BeFalse(because: "original archive is deleted after repacking");
        newArchivePath.Directory.Should().BeNull(because: "struct isn't initialized");
        newArchivePath.FileName.Should().BeNull(because: "struct isn't initialized");
    }

    private async Task<AbsolutePath> CreateInitialArchive(InMemoryFileSystem fs, AbsolutePath folderPath)
    {
        var file1 = folderPath.Combine(File1Name);
        var file2 = folderPath.Combine(File2Name);
        var file3 = folderPath.Combine(File3Name);
        await fs.WriteAllTextAsync(file1, File1Content);
        await fs.WriteAllTextAsync(file2, File2Content);
        await fs.WriteAllTextAsync(file3, File3Content);

        var archivePath = folderPath.Parent.Combine(OriginalArchiveName);
        var builder = new NxPackerBuilder();
        builder.AddFolder(folderPath)
            .WithMaxNumThreads(1) // Content is so small that spawning extra threads is slower
            .WithOutput(archivePath)
            .Build();

        return archivePath;
    }

    private ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> SetupGarbageCollector(AbsolutePath archivePath, out ParsedHeader header)
    {
        var streamProvider = new FromAbsolutePathProvider
        {
            FilePath = archivePath,
        };
        header = HeaderParser.ParseHeader(streamProvider);
        var collector = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        collector.AddArchive(archivePath, new NxParsedHeaderState(header));
        return collector;
    }
    private static void AddFile1AndFile2(ParsedHeader header, ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> collector)
    {
        // Add references to file1 and file2, but not file3
        for (var x = 0; x < header.Entries.Length; x++)
        {
            var entry = header.Entries[x];
            var entryName = header.Pool[x];
            if (entryName is File1Name or File2Name)
                collector.AddReferencedFile((Hash)entry.Hash);
        }
    }
}
