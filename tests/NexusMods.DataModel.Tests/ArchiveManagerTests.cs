using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class ArchiveManagerTests
{
    private readonly ArchiveManager _manager;
    private readonly TemporaryFileManager _temporaryFileManager;

    public ArchiveManagerTests(ArchiveManager manager, TemporaryFileManager temporaryFileManager)
    {
        _manager = manager;
        _temporaryFileManager = temporaryFileManager;
    }

    [Fact]
    public async Task CanArchiveAndOpenFiles()
    {
        await using var file = _temporaryFileManager.CreateFile();
        await file.Path.WriteAllTextAsync("Hello World!");
        var hash = await _manager.ArchiveFile(file.Path);
        hash.Should().Be(Hash.From(0xA52B286A3E7F4D91));

        _manager.HaveArchive(hash).Should().BeTrue();
        _manager.HaveFile(hash).Should().BeTrue();
        (await _manager.Open(hash).ReadAllTextAsync()).Should().Be("Hello World!");

        _manager.AllArchives().Should().Contain(hash);
    }
}