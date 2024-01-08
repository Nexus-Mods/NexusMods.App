using FluentAssertions;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

[Trait("RequiresGameInstalls", "True")] // Technically this doesn't require the game, but the DI system does for the other tests
public class PluginAnalysisTests : AGameTest<SkyrimSpecialEdition>
{
    private readonly AbsolutePath _plugin1;
    private readonly AbsolutePath _plugin2;
    private readonly PluginAnalyzer _pluginAnalyzer;

    public PluginAnalysisTests(IFileSystem fileSystem, PluginAnalyzer pluginAnalyzer, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _pluginAnalyzer = pluginAnalyzer;
        _plugin1 = BethesdaTestHelpers.GetAssetsPath(fileSystem).Combine("testfile1.esp");
        _plugin2 = BethesdaTestHelpers.GetAssetsPath(fileSystem).Combine("testfile2.esl");
    }

    [Fact]
    public async Task LoadsMetadataForPlugins_Esm() => await VerifyDependsOnSkyrimEsm(_plugin1);

    [Fact]
    public async Task LoadsMetadataForPlugins_Esl() => await VerifyDependsOnSkyrimEsm(_plugin2);

    private async Task VerifyDependsOnSkyrimEsm(AbsolutePath path)
    {
        await using var stream = path.Read();
        var resultData = await _pluginAnalyzer.AnalyzeAsync(path.FileName, stream);
        resultData.Should().BeEquivalentTo(new PluginAnalysisData
        {
            IsLightMaster = true,
            Masters = new[] { "Skyrim.esm".ToRelativePath() },
            FileName = path.FileName
        });
    }
}
