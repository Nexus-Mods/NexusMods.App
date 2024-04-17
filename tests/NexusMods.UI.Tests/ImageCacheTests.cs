using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.App.UI;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.UI.Tests;

// NOTE(erri120): This inherits from AUiTest because the Avalonia
// Bitmap class requires Avalonia to be initialized beforehand.
public class ImageCacheTests : AUiTest
{
    private readonly IServiceProvider _serviceProvider;

    public ImageCacheTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_LoadAndCache_RemoteImage()
    {
        const string url = "https://http.cat/418.jpg";
        var uri = new Uri(url);

        using var scope = _serviceProvider.CreateScope();
        using var imageCache = scope.ServiceProvider.GetRequiredService<IImageCache>();

        var image1 = await imageCache.GetImage(new OptionImage(uri), cancellationToken: default);
        image1.Should().NotBeNull();

        var image2 = await imageCache.GetImage(new OptionImage(uri), cancellationToken: default);
        image2.Should().NotBeNull();

        image1.Should().BeSameAs(image2);
    }

    [Fact]
    public async Task Test_LoadAndCache_ImageStoredFile()
    {
        var hash = await PrepareImage();

        using var scope = _serviceProvider.CreateScope();
        using var imageCache = scope.ServiceProvider.GetRequiredService<IImageCache>();

        var image1 = await imageCache.GetImage(new OptionImage(new OptionImage.ImageStoredFile(hash)), cancellationToken: default);
        image1.Should().NotBeNull();

        var image2 = await imageCache.GetImage(new OptionImage(new OptionImage.ImageStoredFile(hash)), cancellationToken: default);
        image2.Should().NotBeNull();

        image1.Should().BeSameAs(image2);
    }

    private async Task<Hash> PrepareImage()
    {
        var archiveManager = _serviceProvider.GetRequiredService<IFileStore>();

        const string url = "https://http.cat/418.jpg";
        var httpClient = new HttpClient();
        var bytes = await httpClient.GetByteArrayAsync(url);

        var hash = bytes.AsSpan().XxHash64();
        var size = Size.FromLong(bytes.LongLength);
        var streamFactory = new MemoryStreamFactory("cat.jpg".ToRelativePath(), new MemoryStream(bytes));

        await archiveManager.BackupFiles(new ArchivedFileEntry[]
        {
            new(streamFactory, hash, size)
        });

        var hasFile = await archiveManager.HaveFile(hash);
        hasFile.Should().BeTrue();

        return hash;
    }
}
