using FluentAssertions;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Hashing.xxHash3.Paths;

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

        var (hash, path) = await _provider.DownloadLatestHashesRelease();

        hash.Should().Be(latestHash.Hash);
        (await path.XxHash3Async()).Should().Be(hash);
    }

    [Fact]
    public async Task CanReadEntries()
    {
        await _provider.DownloadLatestHashesRelease();
        var hashes = await _provider.GetHashes(GameId.From(1303));
        hashes.Should().NotBeEmpty();
    }
}
