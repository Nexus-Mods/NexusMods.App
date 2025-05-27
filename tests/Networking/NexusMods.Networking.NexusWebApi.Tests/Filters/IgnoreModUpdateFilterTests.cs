using NexusMods.Abstractions.NexusModsLibrary;
using Size = NexusMods.Paths.Size;
using FluentAssertions;
using NSubstitute;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi.UpdateFilters;

namespace NexusMods.Networking.NexusWebApi.Tests.Filters;

public class IgnoreModUpdateFilterTests(IConnection connection)
{

    [Fact]
    public async Task SelectMod_WithNoIgnoredFiles_ShouldReturnAllFiles()
    {
        // Arrange
        var fileFilter = Substitute.For<IShouldIgnoreFile>();
        fileFilter.ShouldIgnoreFile(Arg.Any<UidForFile>()).Returns(false);

        var filter = new IgnoreModUpdateFilter<IShouldIgnoreFile>(fileFilter);

        var file1 = await CreateFileMetadataAsync(1);
        var file2 = await CreateFileMetadataAsync(2);
        var modPage = new ModUpdateOnPage(
            File: file1,
            NewerFiles: [file2]
        );

        // Act
        var result = filter.SelectMod(modPage);

        // Assert
        result.Should().NotBeNull();
        result!.Value.NewerFiles.Should().HaveCount(1);
        result.Value.NewerFiles.Should().Contain(file2);
    }

    [Fact]
    public async Task SelectMod_WithIgnoredFiles_ShouldFilterThem()
    {
        // Arrange
        var fileFilter = Substitute.For<IShouldIgnoreFile>();
        var filter = new IgnoreModUpdateFilter<IShouldIgnoreFile>(fileFilter);

        var file1 = await CreateFileMetadataAsync(1);
        var file2 = await CreateFileMetadataAsync(2);
        var file3 = await CreateFileMetadataAsync(3);

        // Setup file2 to be ignored
        fileFilter.ShouldIgnoreFile(file2.Uid).Returns(true);
        fileFilter.ShouldIgnoreFile(Arg.Is<UidForFile>(uid => !uid.Equals(file2.Uid))).Returns(false);

        var modPage = new ModUpdateOnPage(
            File: file1,
            NewerFiles: [file2, file3]
        );

        // Act
        var result = filter.SelectMod(modPage);

        // Assert
        result.Should().NotBeNull();
        result!.Value.NewerFiles.Should().HaveCount(1);
        result.Value.NewerFiles.Should().NotContain(file2);
        result.Value.NewerFiles.Should().Contain(file3);
    }

    [Fact]
    public async Task SelectMod_WithAllIgnoredFiles_ShouldReturnEmptyArray()
    {
        // Arrange
        var fileFilter = Substitute.For<IShouldIgnoreFile>();
        fileFilter.ShouldIgnoreFile(Arg.Any<UidForFile>()).Returns(true);

        var filter = new IgnoreModUpdateFilter<IShouldIgnoreFile>(fileFilter);

        var file1 = await CreateFileMetadataAsync(1);
        var file2 = await CreateFileMetadataAsync(2);
        var modPage = new ModUpdateOnPage(
            File: file1,
            NewerFiles: [file2]
        );

        // Act
        var result = filter.SelectMod(modPage);

        // Assert
        result.Should().NotBeNull();
        result!.Value.NewerFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task SelectModPage_ShouldAlwaysReturnNull()
    {
        // Arrange
        var fileFilter = Substitute.For<IShouldIgnoreFile>();
        var filter = new IgnoreModUpdateFilter<IShouldIgnoreFile>(fileFilter);

        var file1 = await CreateFileMetadataAsync(1);
        var modPageUpdate = new ModUpdateOnPage(
            File: file1,
            NewerFiles: [file1]
        );
        var modPage = new ModUpdatesOnModPage(
            FileMappings: [modPageUpdate]
        );

        // Act
        var result = filter.SelectModPage(modPage);

        // Assert
        result.Should().BeNull();
    }

    private async Task<NexusModsFileMetadata.ReadOnly> CreateFileMetadataAsync(ulong fileId)
    {
        using var tx = connection.BeginTransaction();

        var metadata = new NexusModsFileMetadata.New(tx)
        {
            Name = $"File {fileId}",
            UploadedAt = DateTimeOffset.UtcNow,
            Size = Size.From(fileId),
            Version = fileId.ToString(),
            Uid = UidForFile.FromUlong(fileId),
            ModPageId = NexusModsModPageMetadataId.From(fileId),
        };
        
        var result = await tx.Commit();
        return result.Remap(metadata);
    }
}
