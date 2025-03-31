using DynamicData.Kernel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Tests;

public class FileStoreTests : ACyberpunkIsolatedGameTest<FileStoreTests>
{
    private readonly ILibraryService _libraryService;
    private readonly IFileStore _fileStore;
    private readonly IConnection _connection;

    public FileStoreTests(ITestOutputHelper helper) : base(helper)
    {
        _libraryService = ServiceProvider.GetRequiredService<ILibraryService>();
        _fileStore = ServiceProvider.GetRequiredService<IFileStore>();
        _connection = ServiceProvider.GetRequiredService<IConnection>();
    }

    [Fact]
    [GithubIssue(2944)]
    public async Task DeletingModWithSharedFilesDoesNotCorruptRemainingFiles()
    {
        // Arrange
        var pathA = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine("Lookup Anything 1.48.1-541-1-48-1-1739333325.zip");
        var pathB = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine("Lookup Anything 1.49.0-541-1-49-0-1740624009.zip");

        var libraryItemA = await _libraryService.AddLocalFile(pathA);
        var libraryItemB = await _libraryService.AddLocalFile(pathB);

        // Get the hashes of the files in the library for item B
        var hashesBBefore = LibraryArchiveFileEntry.FindByParent(_connection.Db, libraryItemB)
            .Select(x => x.AsLibraryFile());
        var dataFileBBefore = hashesBBefore.FirstOrOptional(x => x.FileName == "data.json");
        dataFileBBefore.HasValue.Should().BeTrue();
        
        var dataFileBHashBefore = dataFileBBefore.Value.Hash;
        
        await using var dataFileBStreamBefore = await _fileStore.GetFileStream(dataFileBHashBefore);
        var dataFileBCheckedHashBefore = await dataFileBStreamBefore.xxHash3Async();
        dataFileBCheckedHashBefore.Should().Be(dataFileBHashBefore);
        
        // Act
        // Remove item A, and re-add it
        await _libraryService.RemoveItems([libraryItemA.AsLibraryFile().AsLibraryItem()]);
        var libraryItemA2 = await _libraryService.AddLocalFile(pathA);
        
        // Assert
        // Removing item A and re-adding it should not affect item B

        var hashesBAfter = LibraryArchiveFileEntry.FindByParent(_connection.Db, libraryItemB)
            .Select(x => x.AsLibraryFile());
        
        var dataFileBAfter = hashesBAfter.FirstOrOptional(x => x.FileName == "data.json");
        dataFileBAfter.HasValue.Should().BeTrue();

        await using var dataFileBStreamAfter = await _fileStore.GetFileStream(dataFileBAfter.Value.Hash);
        var dataFileBCheckedHashAfter = await dataFileBStreamAfter.xxHash3Async();
        
        dataFileBCheckedHashAfter.Should().Be(dataFileBHashBefore);
    }
}
