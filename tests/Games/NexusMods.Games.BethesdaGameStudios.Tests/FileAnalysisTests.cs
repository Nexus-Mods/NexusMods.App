using FluentAssertions;
using NexusMods.DataModel;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

[Trait("RequiresGameInstalls", "True")] // Technically this doesn't require the game, but the DI system does for the other tests
public class FileAnalysisTests
{
    private readonly FileContentsCache _cache;
    private readonly AbsolutePath _plugin1;
    private readonly AbsolutePath _plugin2;

    public FileAnalysisTests(FileContentsCache cache, IFileSystem fileSystem)
    {
        _cache = cache;
        _plugin1 = fileSystem.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked("Resources").CombineUnchecked("testfile1.esp");
        _plugin2 = fileSystem.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked("Resources").CombineUnchecked("testfile2.esl");
    }

    [Fact]
    public async Task LoadsMetadataForPlugins_Esm() => VerifyDependsOnSkyrimEsm(await _cache.AnalyzeFileAsync(_plugin1));

    [Fact]
    public async Task LoadsMetadataForPlugins_Esl() => VerifyDependsOnSkyrimEsm(await _cache.AnalyzeFileAsync(_plugin2));

    private static void VerifyDependsOnSkyrimEsm(AnalyzedFile result)
    {
        result.AnalysisData.Should().ContainEquivalentOf(new PluginAnalysisData
        {
            IsLightMaster = true,
            Masters = new[] { "Skyrim.esm".ToRelativePath() },
        });
    }
}
