using System.Collections.Generic;
using FluentAssertions;
using NexusMods.App.GarbageCollection.Errors;
using NexusMods.App.GarbageCollection.Structs;
using NexusMods.App.GarbageCollection.Tests.Helpers;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using Xunit;
namespace NexusMods.App.GarbageCollection.Tests;

public class ArchiveGarbageCollectorTests
{
    [Theory, AutoFileSystem]
    public void AddFiles_ShouldAddAllHashes(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState = new MockParsedHeaderState(hash1, hash2);

        // Act
        collector.AddArchive(archivePath, headerState);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);

        // Assert

        // Contains the hashes
        collector.HashToArchive[hash1].FilePath.Should().Be(archivePath);
        collector.HashToArchive[hash2].FilePath.Should().Be(archivePath);

        // The inner archive 
        collector.HashToArchive[hash1].Entries.Should().ContainKey(hash1);
        collector.HashToArchive[hash1].Entries.Should().ContainKey(hash2);

        // Check the ref count
        collector.HashToArchive[hash1].Entries[hash1].GetRefCount().Should().Be(1);
        collector.HashToArchive[hash2].Entries[hash2].GetRefCount().Should().Be(1);

        // Check archive count
        collector.AllArchives.Count.Should().Be(1);
    }

    [Theory, AutoFileSystem]
    public void AddFiles_WithMultipleReferences_ShouldIncreaseRefCount(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState = new MockParsedHeaderState(hash1, hash2);

        // Act
        collector.AddArchive(archivePath, headerState);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);

        // Assert

        // Check the ref count
        collector.HashToArchive[hash1].Entries[hash1].GetRefCount().Should().Be(2);
        collector.HashToArchive[hash2].Entries[hash2].GetRefCount().Should().Be(2);
    }

    [Fact]
    public void AddReferencedFile_ShouldThrowForUnknownHash()
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var unknownHash = (Hash)999;

        // Act & Assert
        var act = () => collector.AddReferencedFile(unknownHash, true);
        act.Should().Throw<UnknownFileException>();
    }

    [Theory, AutoFileSystem]
    public void AddArchive_ShouldHandleMultipleArchives(AbsolutePath path1, AbsolutePath path2)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState1 = new MockParsedHeaderState(hash1);
        var headerState2 = new MockParsedHeaderState(hash2);

        // Act
        collector.AddArchive(path1, headerState1);
        collector.AddArchive(path2, headerState2);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);

        // Assert
        // Verify that each hash is associated with the correct archive path
        collector.HashToArchive[hash1].FilePath.Should().Be(path1);
        collector.HashToArchive[hash2].FilePath.Should().Be(path2);

        // Verify that each hash has the correct reference count
        collector.HashToArchive[hash1].Entries[hash1].GetRefCount().Should().Be(1);
        collector.HashToArchive[hash2].Entries[hash2].GetRefCount().Should().Be(1);

        // Verify that each archive has the correct HeaderState
        collector.HashToArchive[hash1].HeaderState.Should().BeSameAs(headerState1);
        collector.HashToArchive[hash2].HeaderState.Should().BeSameAs(headerState2);

        // Verify that each archive only contains its own hash
        collector.HashToArchive[hash1].Entries.Should().NotContainKey(hash2);
        collector.HashToArchive[hash2].Entries.Should().NotContainKey(hash1);

        // Verify the archive count
        collector.AllArchives.Count.Should().Be(2);
    }

    [Theory, AutoFileSystem]
    public void CollectGarbage_ShouldNotRepackWhenAllFilesReferenced(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState = new MockParsedHeaderState(hash1, hash2);

        collector.AddArchive(archivePath, headerState);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);

        var repackCalled = false;

        // Act
        collector.CollectGarbage(null!, (_, _, _, _) =>
        {
            repackCalled = true;
        });

        // Assert
        repackCalled.Should().BeFalse();
    }

    [Theory, AutoFileSystem]
    public void CollectGarbage_ShouldRepackWhenSomeFilesUnreferenced(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState = new MockParsedHeaderState(hash1, hash2);

        collector.AddArchive(archivePath, headerState);
        collector.AddReferencedFile(hash1);
        // hash2 is not referenced

        List<Hash> toBeArchived = null!;
        List<Hash> toBeRemoved = null!;
        ArchiveReference<MockParsedHeaderState> repackedArchive = null!;

        // Act
        collector.CollectGarbage(null!, (_, toArchive, toRemove, archive) =>
        {
            toBeArchived = toArchive;
            toBeRemoved = toRemove;
            repackedArchive = archive;
        });

        // Assert
        toBeArchived.Should().NotBeNull();
        toBeArchived.Should().ContainSingle();
        toBeArchived[0].Should().Be(hash1);
        
        toBeRemoved.Should().NotBeNull();
        toBeRemoved.Should().ContainSingle();
        toBeRemoved[0].Should().Be(hash2);
        
        repackedArchive.Should().NotBeNull();
        repackedArchive.FilePath.Should().Be(archivePath);
    }

    [Theory, AutoFileSystem]
    public void CollectGarbage_ShouldHandleMultipleArchives(AbsolutePath path1, AbsolutePath path2)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var hash3 = (Hash)3;
        var hash4 = (Hash)4;
        var headerState1 = new MockParsedHeaderState(hash1, hash2);
        var headerState2 = new MockParsedHeaderState(hash3, hash4);

        collector.AddArchive(path1, headerState1);
        collector.AddArchive(path2, headerState2);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash3);
        collector.AddReferencedFile(hash4);
        // hash2 is not referenced

        AbsolutePath? repackedArchives = null;
        List<Hash>? toBeArchived = null;
        List<Hash>? toBeRemoved = null;

        // Act
        collector.CollectGarbage(null!, (_, toArchive, toRemove, archive) =>
        {
            if (repackedArchives != null || toBeArchived != null || toBeRemoved != null)
                Assert.Fail("Repack called multiple times. Only one archive should be repacked.");

            repackedArchives = archive.FilePath;
            toBeArchived = toArchive;
            toBeRemoved = toRemove;
        });

        // Assert
        repackedArchives.Should().Be(path1);
        toBeArchived.Should().ContainSingle();
        toBeArchived![0].Should().Be(hash1);
        toBeRemoved.Should().ContainSingle();
        toBeRemoved![0].Should().Be(hash2);
    }

    [Theory, AutoFileSystem]
    public void CollectGarbage_ShouldHandleEmptyArchives(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var headerState = new MockParsedHeaderState();

        collector.AddArchive(archivePath, headerState);

        var repackCalled = false;

        // Act
        collector.CollectGarbage(null!, (_, _, _, _) =>
        {
            repackCalled = true;
        });

        // Assert
        repackCalled.Should().BeFalse();
    }

    [Theory, AutoFileSystem]
    public void CollectGarbage_ShouldHandleAllUnreferencedFiles(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState, MockFileHash>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState = new MockParsedHeaderState(hash1, hash2);

        collector.AddArchive(archivePath, headerState);

        // No files are referenced (AddReferencedFile not called)
        List<Hash> toBeArchived = null!;
        List<Hash> toBeRemoved = null!;

        // Act
        collector.CollectGarbage(null!, (_, toArchive, toRemove, _) =>
        {
            toBeArchived = toArchive;
            toBeRemoved = toRemove;
        });

        // Assert
        toBeArchived.Should().NotBeNull();
        toBeArchived.Should().BeEmpty();
        
        toBeRemoved.Should().NotBeNull();
        toBeRemoved.Should().Contain(hash1);
        toBeRemoved.Should().Contain(hash2);
    }
}
