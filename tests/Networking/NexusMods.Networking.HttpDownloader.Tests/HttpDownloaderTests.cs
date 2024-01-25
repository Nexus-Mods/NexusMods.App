using FluentAssertions;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.Tests;


public class HttpDownloaderTests
{
    private readonly IHttpDownloader _httpDownloader;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly LocalHttpServer _localHttpServer;

    public HttpDownloaderTests(SimpleHttpDownloader httpDownloader, TemporaryFileManager temporaryFileManager, LocalHttpServer localHttpServer)
    {
        _httpDownloader = httpDownloader;
        _temporaryFileManager = temporaryFileManager;
        _localHttpServer = localHttpServer;
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task CanDownloadFromExternalSource()
    {
        await using var path = _temporaryFileManager.CreateFile();

        var resultHash = await _httpDownloader.DownloadAsync(new[]
        {
            "http://miami.nexus-cdn.com/100M",
            "http://la.nexus-cdn.com/100M",
            "http://paris.nexus-cdn.com/100M",
            "http://chicago.nexus-cdn.com/100M"
        }.Select(x => new HttpRequestMessage(HttpMethod.Get, new Uri(x)))
        .ToArray(), path);

        resultHash.Should().Be(Hash.From(0xBEEADB5B05BED390));
    }

    [Fact]
    public async Task CanDownloadFromLocalServer()
    {
        await using var path = _temporaryFileManager.CreateFile();

        var resultHash = await _httpDownloader.DownloadAsync(new[]
        {
            new HttpRequestMessage(HttpMethod.Get, _localHttpServer.Uri + "hello")
        }, path);

        resultHash.Should().Be(Hash.From(0xA52B286A3E7F4D91));
        (await path.Path.ReadAllTextAsync()).Should().Be("Hello World!");
    }
}
