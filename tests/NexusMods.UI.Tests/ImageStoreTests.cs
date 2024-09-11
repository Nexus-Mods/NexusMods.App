using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Media;

namespace NexusMods.UI.Tests;

public class ImageStoreTests : AUiTest
{
    private readonly IImageStore _imageStore;

    public ImageStoreTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _imageStore = serviceProvider.GetRequiredService<IImageStore>();
    }

    [Fact]
    public async Task SimpleTest()
    {
        var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        var storedImage = await _imageStore.PutAsync(bitmap);
        using var lifetime = _imageStore.Get(storedImage);
        lifetime.Should().NotBeNull();

        var result = lifetime!.Value;
        result.PixelSize.Equals(bitmap.PixelSize).Should().BeTrue();
        result.Dpi.NearlyEquals(bitmap.Dpi).Should().BeTrue();
        result.AlphaFormat.Should().Be(bitmap.AlphaFormat);
        result.Format.Should().NotBeNull();
        result.Format!.Value.ToSkColorType().Should().Be(bitmap.Format!.Value.ToSkColorType());
    }
}
