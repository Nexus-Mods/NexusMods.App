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
    private readonly IGarbageCollectorRunner _gcRunner;

    public FileStoreTests(ITestOutputHelper helper) : base(helper)
    {
        _libraryService = ServiceProvider.GetRequiredService<ILibraryService>();
        _fileStore = ServiceProvider.GetRequiredService<IFileStore>();
        _connection = ServiceProvider.GetRequiredService<IConnection>();
        _gcRunner = ServiceProvider.GetRequiredService<IGarbageCollectorRunner>();
    }
    
    
    [Fact]
    [GithubIssue(2944)]
    public async Task DeletingModWithSharedFileDoesNotCorrupt()
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
        
        // Act
        await _libraryService.RemoveItems([libraryItemA.AsLibraryFile().AsLibraryItem()], GarbageCollectorRunMode.RunSynchronously);
        
        // Assert
        // Changing A should not affect B

        var hashesBAfter = LibraryArchiveFileEntry.FindByParent(_connection.Db, libraryItemB)
            .Select(x => x.AsLibraryFile());
        
        var dataFileBAfter = hashesBAfter.FirstOrOptional(x => x.FileName == "data.json");
        dataFileBAfter.HasValue.Should().BeTrue();
        
        dataFileBAfter.Value.Hash.Should().Be(dataFileBHashBefore, "Hash should be the same after changing A");

        {
            await using var dataFileBStreamAfter = await _fileStore.GetFileStream(dataFileBAfter.Value.Hash);
            var dataFileBCheckedHashAfter = await dataFileBStreamAfter.xxHash3Async();
            dataFileBCheckedHashAfter.Should().Be(dataFileBHashBefore, "Hash should be the same after changing A");
        }
    }
}
