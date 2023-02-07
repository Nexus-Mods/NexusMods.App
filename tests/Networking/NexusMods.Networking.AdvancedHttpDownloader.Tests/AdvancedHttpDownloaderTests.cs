using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusMods.Networking.AdvancedHttpDownloader.Tests;

public class AdvancedHttpDownloaderTests
{
    private readonly IHttpDownloader _httpDownloader;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly LocalHttpServer _localHttpServer;

    public AdvancedHttpDownloaderTests(IHttpDownloader httpDownloader, TemporaryFileManager temporaryFileManager, LocalHttpServer localHttpServer)
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

        var resultHash = await _httpDownloader.Download(new[]
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

        var resultHash = await _httpDownloader.Download(new[]
        {
            new HttpRequestMessage(HttpMethod.Get, _localHttpServer.Uri + "hello")
        }, path);

        resultHash.Should().Be(Hash.From(0xA52B286A3E7F4D91));
        (await path.Path.ReadAllTextAsync()).Should().Be("Hello World!");
    }

    [Fact]
    public async Task CanResumeDownload()
    {
        await using var path = _temporaryFileManager.CreateFile();

        const string existingContent = "Welcome back, ";
        const string missingContent = "World!";

        await path.Path.ReplaceExtension(new Extension(".downloading")).WriteAllTextAsync(existingContent);
        await path.Path.ReplaceExtension(new Extension(".progress")).WriteAllTextAsync(JsonSerializer.Serialize(new
        {
            totalSize = existingContent.Length + missingContent.Length,
            chunks = new object[]
            {
                new
                {
                    offset = 0,
                    size = existingContent.Length + missingContent.Length,
                    completed = existingContent.Length,
                    initChunk = true,
                }
            }
        }));

        var resultHash = await _httpDownloader.Download(new[]
        {
            new HttpRequestMessage(HttpMethod.Get, _localHttpServer.Uri + "resume")
        }, path);

        resultHash.Should().Be(Hash.From(0x79B2246476DAA5CE));
        (await path.Path.ReadAllTextAsync()).Should().Be("Welcome back, World!");
    }
}
