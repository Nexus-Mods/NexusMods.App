using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Networking.HttpDownloader.DTOs;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class AdvancedHttpDownloaderTests
{
    private readonly IHttpDownloader _httpDownloader;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly LocalHttpServer _localHttpServer;

    public AdvancedHttpDownloaderTests(AdvancedHttpDownloader httpDownloader, TemporaryFileManager temporaryFileManager, LocalHttpServer localHttpServer)
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

    [Fact]
    public async Task CanResumeDownload()
    {
        await using var path = _temporaryFileManager.CreateFile();

        var tokenSource = new CancellationTokenSource();

        var sources = new[]
        {
            "http://miami.nexus-cdn.com/100M",
            "http://la.nexus-cdn.com/100M",
            "http://paris.nexus-cdn.com/100M",
            "http://chicago.nexus-cdn.com/100M"
        }.Select(x => new HttpRequestMessage(HttpMethod.Get, new Uri(x))).ToArray();

        var downloadTask = _httpDownloader.DownloadAsync(sources,path, token: tokenSource.Token);

        var progressFile = DownloadState.GetStateFilePath(path.Path);
        var downloadingFile = DownloadState.GetTempFilePath(path.Path);

        while (!progressFile.FileExists)
            await Task.Delay(10);

        tokenSource.Cancel();
        try
        {
            await downloadTask;
        }
        catch (TaskCanceledException)
        {
            // expected
        }

        path.Path.FileExists.Should().BeFalse("The file hasn't fully downloaded yet");
        progressFile.FileExists.Should().BeTrue();
        downloadingFile.FileExists.Should().BeTrue();




        tokenSource = new CancellationTokenSource();
        var hash = await _httpDownloader.DownloadAsync(sources, path, token: tokenSource.Token);

        hash.Should().Be(Hash.From(0xBEEADB5B05BED390));

    }

    [Fact]
    public async Task CanDownloadFromReliableSource()
    {
        await using var path = _temporaryFileManager.CreateFile();

        var resultHash = await _httpDownloader.DownloadAsync(new[]
        {
            new HttpRequestMessage(HttpMethod.Get, _localHttpServer.Uri + "reliable")
        }, path);

        resultHash.Should().Be(_localHttpServer.LargeDataHash);
    }

    [Fact]
    public async Task CanDownloadFromUnreliableSource()
    {
        await using var path = _temporaryFileManager.CreateFile();

        var resultHash = await _httpDownloader.DownloadAsync(new[]
        {
            new HttpRequestMessage(HttpMethod.Get, _localHttpServer.Uri + "unreliable")
        }, path);

        path.Path.FileInfo.Size.Value.Should().Be((ulong)_localHttpServer.LargeData.Length);
        resultHash.Should().Be(_localHttpServer.LargeDataHash);
    }
}
