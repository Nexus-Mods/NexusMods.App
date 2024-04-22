using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.IO;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NSubstitute;

namespace NexusMods.DataModel.Tests;

public class FileOriginRegistryTests(IServiceProvider provider)
    : ADataModelTest<FileOriginRegistryTests>(provider)
{
    [Fact]
    public async Task RegisterFolder_ShouldRegisterCorrectly()
    {
        // Arrange
        IFileOriginRegistry sut = new FileOriginRegistry(
            ServiceProvider.GetRequiredService<ILogger<FileOriginRegistry>>(),
            ServiceProvider.GetRequiredService<IFileExtractor>(),
            FileStore,
            TemporaryFileManager,
            Connection,
            ServiceProvider.GetRequiredService<IFileHashCache>());
        
        // Act
        var result = await sut.RegisterDownload(DataZipLzma, CancellationToken.None);

        // Assert
        var analysis = FileOriginRegistry.Get(result);
        analysis.Hash.Should().Be(Hash.From(0x706F72D12A82892D));
        analysis!.Contents.Should().ContainSingle(x => x.Hash == Hash.From(3737353793016823850));
        analysis.Contents.Should().ContainSingle(x => x.Hash == Hash.From(14547888027026727014));
        analysis.Contents.Should().ContainSingle(x => x.Hash == Hash.From(16430723325827464675));

        (await FileStore.HaveFile(Hash.From(3737353793016823850))).Should().BeTrue();
        (await FileStore.HaveFile(Hash.From(14547888027026727014))).Should().BeTrue();
        (await FileStore.HaveFile(Hash.From(16430723325827464675))).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterFolder_WhenCalledTwice_ShouldBeDedupedOnSameArchive()
    {
        // Arrange
        var fileStore = Substitute.For<IFileStore>();
        fileStore.BackupFiles(Arg.Any<IEnumerable<ArchivedFileEntry>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        fileStore.GetFileHashes().Returns(new HashSet<ulong>()); // not needed here
        IFileOriginRegistry sut = new FileOriginRegistry(
            ServiceProvider.GetRequiredService<ILogger<FileOriginRegistry>>(),
            ServiceProvider.GetRequiredService<IFileExtractor>(),
            fileStore,
            TemporaryFileManager,
            Connection,
            ServiceProvider.GetRequiredService<IFileHashCache>());


        
        // Act
        await sut.RegisterDownload(DataZipLzma, CancellationToken.None);
        await sut.RegisterDownload(DataZipLzma, CancellationToken.None);

        // BackupFiles should only have been called once if all files were duplicate.
        await fileStore.Received(1)
            .BackupFiles(Arg.Any<IEnumerable<ArchivedFileEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public async Task RegisterFolder_WhenCalledTwice_ShouldOnlyAppendNewData()
    {
        // Arrange
        var fileStore = Substitute.For<IFileStore>();

        IFileOriginRegistry sut = new FileOriginRegistry(
            ServiceProvider.GetRequiredService<ILogger<FileOriginRegistry>>(),
            ServiceProvider.GetRequiredService<IFileExtractor>(),
            fileStore,
            TemporaryFileManager,
            Connection,
            ServiceProvider.GetRequiredService<IFileHashCache>());

        // Act
        var capturedCalls = new List<IEnumerable<ArchivedFileEntry>>();
        var hashSet = new HashSet<ulong>(); // stores hashes of files we backed up 'so far'.

        fileStore.GetFileHashes().Returns(hashSet);
        await fileStore.BackupFiles(Arg.Do<IEnumerable<ArchivedFileEntry>>(entries =>
            {
                capturedCalls.Add(entries);

                // We backed up a file, pretend we put it in DataStore.
                foreach (var entry in entries)
                    hashSet.Add(entry.Hash.Value);
            }),
            Arg.Any<CancellationToken>());

        await sut.RegisterDownload(DataZipLzma, CancellationToken.None);

        // Act: We now add Mod V2
        await sut.RegisterDownload(DataZipLzmaWithExtraFile, CancellationToken.None);

        // Assert
        // Ensure there were at least two calls
        (capturedCalls.Count >= 2).Should().BeTrue("BackupFiles should have been called at least twice.");

        // Check the second call for new file
        var secondCallEntries = capturedCalls[1];
        secondCallEntries.Count().Should().Be(1, "Only one file is new.");
        secondCallEntries.Should().Contain(entry => entry.Hash == Hash.From(2001900376554900883),
            "the second call to BackupFiles should include an ArchivedFileEntry with the specified hash.");
    }
}
