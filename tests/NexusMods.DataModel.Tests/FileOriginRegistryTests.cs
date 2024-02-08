using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NSubstitute;

namespace NexusMods.DataModel.Tests;

public class FileOriginRegistryTests : ADataModelTest<FileOriginRegistryTests>
{
    public FileOriginRegistryTests(IServiceProvider provider) : base(provider)
    {

    }

    [Fact]
    public async Task RegisterFolder_ShouldRegisterCorrectly()
    {
        // Arrange
        var sut = new FileOriginRegistry(
            ServiceProvider.GetRequiredService<ILogger<FileOriginRegistry>>(),
            ServiceProvider.GetRequiredService<IFileExtractor>(),
            FileStore,
            TemporaryFileManager,
            DataStore,
            ServiceProvider.GetRequiredService<IFileHashCache>());

        var metaData = new MockArchiveMetadata
        {
            Name = "MockArchive",
            Quality = Quality.Highest
        };

        // Act
        var result = await sut.RegisterDownload(DataZipLzma, metaData, CancellationToken.None);

        // Assert
        var analysis = DataStore.Get<DownloadAnalysis>(IId.From(EntityCategory.DownloadMetadata, result.Value));
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
        var sut = new FileOriginRegistry(
            ServiceProvider.GetRequiredService<ILogger<FileOriginRegistry>>(),
            ServiceProvider.GetRequiredService<IFileExtractor>(),
            fileStore,
            TemporaryFileManager,
            DataStore,
            ServiceProvider.GetRequiredService<IFileHashCache>());

        var metaData = new MockArchiveMetadata
        {
            Name = "MockArchive",
            Quality = Quality.Highest
        };

        // Act
        await sut.RegisterDownload(DataZipLzma, metaData, CancellationToken.None);
        await sut.RegisterDownload(DataZipLzma, metaData, CancellationToken.None);

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
        fileStore.BackupFiles(Arg.Any<IEnumerable<ArchivedFileEntry>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var sut = new FileOriginRegistry(
            ServiceProvider.GetRequiredService<ILogger<FileOriginRegistry>>(),
            ServiceProvider.GetRequiredService<IFileExtractor>(),
            fileStore,
            TemporaryFileManager,
            DataStore,
            ServiceProvider.GetRequiredService<IFileHashCache>());

        // Act
        var metaData = new MockArchiveMetadata
        {
            Name = "MockArchive",
            Quality = Quality.Highest
        };

        var capturedCalls = new List<IEnumerable<ArchivedFileEntry>>();
        await fileStore.BackupFiles(Arg.Do<IEnumerable<ArchivedFileEntry>>(entries => capturedCalls.Add(entries)),
            Arg.Any<CancellationToken>());

        await sut.RegisterDownload(DataZipLzma, metaData, CancellationToken.None);

        // Act: We now add Mod V2
        var metaData2 = new MockArchiveMetadata
        {
            Name = "MockArchive v2",
            Quality = Quality.Highest
        };

        await sut.RegisterDownload(DataZipLzmaWithExtraFile, metaData2, CancellationToken.None);

        // Assert
        // Ensure there were at least two calls
        (capturedCalls.Count >= 2).Should().BeTrue("BackupFiles should have been called at least twice.");

        // Check the second call for new file
        var secondCallEntries = capturedCalls[1];
        secondCallEntries.Count().Should().Be(1, "Only one file is new.");
        secondCallEntries.Should().Contain(entry => entry.Hash == Hash.From(2001900376554900883),
            "the second call to BackupFiles should include an ArchivedFileEntry with the specified hash.");
    }

    [JsonName("NexusMods.DataModel.Tests.FileOriginRegistryTests.MockArchiveMetadta")]
    public record MockArchiveMetadata : AArchiveMetaData { }
}
