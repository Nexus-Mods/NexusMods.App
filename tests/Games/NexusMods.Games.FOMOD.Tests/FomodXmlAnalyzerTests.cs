﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlAnalyzerTests
{
    private FomodAnalyzer _fomodAnalyzer;

    public FomodXmlAnalyzerTests(IServiceProvider provider)
    {
        _fomodAnalyzer = new FomodAnalyzer(provider.GetService<ILogger<FomodAnalyzer>>()!, FileSystem.Shared);
    }

    // Tests whether
    [Fact]
    public async Task AnalyzeAsync_CachesXML()
    {
        var result = await FomodTestHelpers.GetXmlPathAndStreamAsync("SimpleInstaller");
        var parentArchive = result.path.Parent.Parent;
        var results = _fomodAnalyzer.AnalyzeAsync(new FileAnalyzerInfo()
        {
            Stream = result.stream,
            FileName = result.path.FileName,
            RelativePath = result.path.RelativeTo(parentArchive),
            ParentArchive = new TemporaryPath(parentArchive, false)
        });

        (await results.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeAsync_WithIncorrectRelativePath_DoesNotCacheXML()
    {
        var result = await FomodTestHelpers.GetXmlPathAndStreamAsync("SimpleInstaller");
        var parentArchive = result.path.Parent.Parent;
        var results = _fomodAnalyzer.AnalyzeAsync(new FileAnalyzerInfo()
        {
            Stream = result.stream,
            FileName = result.path.FileName,
            ParentArchive = new TemporaryPath(parentArchive, false)
        });

        (await results.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_WithoutParentArchive_DoesNotCacheXML()
    {
        var result = await FomodTestHelpers.GetXmlPathAndStreamAsync("SimpleInstaller");
        var parentArchive = result.path.Parent.Parent;
        var results = _fomodAnalyzer.AnalyzeAsync(new FileAnalyzerInfo()
        {
            Stream = result.stream,
            FileName = result.path.FileName,
            RelativePath = result.path.RelativeTo(parentArchive)
        });

        (await results.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_WithImages_CachesImages()
    {
        var result = await FomodTestHelpers.GetXmlPathAndStreamAsync("WithImages");
        var parentArchive = result.path.Parent.Parent;
        var results = _fomodAnalyzer.AnalyzeAsync(new FileAnalyzerInfo()
        {
            Stream = result.stream,
            FileName = result.path.FileName,
            RelativePath = result.path.RelativeTo(parentArchive),
            ParentArchive = new TemporaryPath(parentArchive, false)
        });

        var items = await results.ToArrayAsync();
        (items.Length).Should().Be(1);
        (items[0].GetType()).Should().Be(typeof(FomodAnalyzerInfo));
        var info = (FomodAnalyzerInfo)items[0];

        // Validate images with order
        info.Images[0].Path.Should().Be("fomod/moduleTitle.png");
        info.Images[1].Path.Should().Be("fomod/g1p1i1.png");
        info.Images[2].Path.Should().Be("fomod/g1p2i1.png");
    }

    [Fact]
    public async Task AnalyzeAsync_WithImages_ReplacesMissingImageWithDummy()
    {
        // g1p2i1 missing
        var result = await FomodTestHelpers.GetXmlPathAndStreamAsync("WithMissingImage");
        var parentArchive = result.path.Parent.Parent;
        var results = _fomodAnalyzer.AnalyzeAsync(new FileAnalyzerInfo()
        {
            Stream = result.stream,
            FileName = result.path.FileName,
            RelativePath = result.path.RelativeTo(parentArchive),
            ParentArchive = new TemporaryPath(parentArchive, false)
        });

        var items = await results.ToArrayAsync();
        (items.Length).Should().Be(1);
        (items[0].GetType()).Should().Be(typeof(FomodAnalyzerInfo));
        var info = (FomodAnalyzerInfo)items[0];
        info.Images.Count.Should().Be(3);

        // Placeholder injected.
        info.Images.Last().Image.Should().Equal(await _fomodAnalyzer.GetPlaceholderImage());
    }
}

