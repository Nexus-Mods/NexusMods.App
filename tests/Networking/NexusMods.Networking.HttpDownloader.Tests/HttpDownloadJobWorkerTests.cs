using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class HttpDownloadJobWorkerTests
{
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IServiceProvider _serviceProvider;

    public HttpDownloadJobWorkerTests(IServiceProvider serviceProvider)
    {
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _serviceProvider = serviceProvider;
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_NexusModsCDN100MFile()
    {
        const string url = "https://paris.nexus-cdn.com/100M";

        await using var outputPath = _temporaryFileManager.CreateFile();
        _ = await HttpDownload.Create(_serviceProvider, new Uri(url), new Uri(url), outputPath.Path);;

        outputPath.Path.FileExists.Should().BeTrue();
        outputPath.Path.FileInfo.Size.Should().Be(Size.MB * 100);

        var hash = await outputPath.Path.XxHash64Async();
        hash.Should().Be(Hash.From(0xBEEADB5B05BED390));
    }
}
