using FluentAssertions;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Games.FileHashes.Tests;

public class DownloadTests
{
    private readonly FileHashProvider _provider;

    public DownloadTests(FileHashProvider provider)
    {
        _provider = provider;
    }
    
    [Fact]
    public async Task CanDownloadHashes()
    {
        var latestHash = await _provider.GetCurrentGithubVersion();

        var downloaded = await _provider.DownloadLatestHashesRelease();

        downloaded.Hash.Should().Be(latestHash.Hash);
    }
}
