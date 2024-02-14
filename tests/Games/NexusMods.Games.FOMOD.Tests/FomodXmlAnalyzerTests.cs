using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlAnalyzerTests
{
    private readonly IFileSystem _fileSystem;

    public FomodXmlAnalyzerTests(IServiceProvider provider)
    {
        _fileSystem = provider.GetRequiredService<IFileSystem>();
    }

    private async ValueTask<FomodAnalyzerInfo?> Analyze(string modName, CancellationToken ct = default)
    {
        var allFiles = await FomodTestHelpers.GetFomodTree(modName);
        var info = await FomodAnalyzer.AnalyzeAsync(allFiles, _fileSystem, ct);
        return info;
    }

    // Tests whether
    [Fact]
    public async Task AnalyzeAsync_CanAnalyzeXML()
    {
        var info = await Analyze("SimpleInstaller");

        info.Should().NotBeNull();
        info!.XmlScript.Should().NotBe("");
    }

    [Fact]
    public async Task AnalyzeAsync_WithImages_CachesImages()
    {
        var info = await Analyze("WithImages");

        info.Should().NotBeNull();

        // Validate images with order
        info!.Images[0].Path.Should().Be("fomod/moduleTitle.png");
        info.Images[1].Path.Should().Be("fomod/g1p1i1.png");
        info.Images[2].Path.Should().Be("fomod/g1p2i1.png");

        // Validate that the images have not been replaced with the placeholder (meaning they were not found).
        var placeholder = await FomodAnalyzer.GetPlaceholderImage(FileSystem.Shared);

        info.Images[0].Image.Should().NotEqual(placeholder);
        info.Images[1].Image.Should().NotEqual(placeholder);
        info.Images[2].Image.Should().NotEqual(placeholder);
    }



    [Fact]
    public async Task AnalyzeAsync_WithImages_ReplacesMissingImageWithDummy()
    {
        // g1p2i1 missing
        var info = await Analyze("WithMissingImage");
        info.Should().NotBeNull();
        info!.Images.Count.Should().Be(3);

        // First two images should be valid, last one should be the placeholder.
        var placeholder = await FomodAnalyzer.GetPlaceholderImage(FileSystem.Shared);

        info.Images[0].Image.Should().NotEqual(placeholder);
        info.Images[1].Image.Should().NotEqual(placeholder);

        // Should be missing, so it should be the placeholder.
        info.Images.Last().Image.Should().Equal(placeholder);
    }

}

