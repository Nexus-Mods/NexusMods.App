using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;

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
        var get = _imageStore.Get(storedImage);
        get.Should().NotBeNull();
    }
}
