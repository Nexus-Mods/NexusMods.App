using FluentAssertions;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

[Trait("RequiresGameInstalls", "True")] // Technically this doesn't require the game, but the DI system does for the other tests
public class PluginAnalysisTests : AFileAnalyzerTest<SkyrimSpecialEdition, PluginAnalyzer>
{
    private readonly AbsolutePath _plugin1;
    private readonly AbsolutePath _plugin2;

    public PluginAnalysisTests(IFileSystem fileSystem, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _plugin1 = BethesdaTestHelpers.GetAssetsPath(fileSystem).CombineUnchecked("testfile1.esp");
        _plugin2 = BethesdaTestHelpers.GetAssetsPath(fileSystem).CombineUnchecked("testfile2.esl");
    }

    [Fact]
    public async Task LoadsMetadataForPlugins_Esm() => VerifyDependsOnSkyrimEsm(await AnalyzeFile(_plugin1));

    [Fact]
    public async Task LoadsMetadataForPlugins_Esl() => VerifyDependsOnSkyrimEsm(await AnalyzeFile(_plugin2));

    private static void VerifyDependsOnSkyrimEsm(IEnumerable<IFileAnalysisData> result)
    {
        result.Should().ContainEquivalentOf(new PluginAnalysisData
        {
            IsLightMaster = true,
            Masters = new[] { "Skyrim.esm".ToRelativePath() },
        });
    }
}
