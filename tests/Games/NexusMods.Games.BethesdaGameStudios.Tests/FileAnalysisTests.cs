using FluentAssertions;
using NexusMods.DataModel;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

[Trait("RequiresGameInstalls", "True")] // Technically this doesn't require the game, but the DI system does for the other tests
public class FileAnalysisTests
{
    private readonly FileContentsCache _cache;
    private readonly AbsolutePath _plugin1;
    private readonly AbsolutePath _plugin2;

    public FileAnalysisTests(FileContentsCache cache)
    {
        _cache = cache;
        _plugin1 = KnownFolders.EntryFolder.Join("Resources", "testfile1.esp");
        _plugin2 = KnownFolders.EntryFolder.Join("Resources", "testfile2.esl");
    }

    [Fact]
    public async Task LoadsMetadataForPlugins()
    {
        var result = await _cache.AnalyzeFile(_plugin1);

        result.AnalysisData.Should().ContainEquivalentOf(new PluginAnalysisData
        {
            IsLightMaster = true,
            Masters = new []{ "Skyrim.esm".ToRelativePath() },
        });
    }
}