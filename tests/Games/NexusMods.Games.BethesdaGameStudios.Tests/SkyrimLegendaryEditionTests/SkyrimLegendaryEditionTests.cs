using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.TestFramework;
using NexusMods.CLI.Tests.VerbTests;
using NexusMods.Paths;


namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimLegendaryEditionTests;

public class SkyrimLegendaryEditionTests : AGameTest<SkyrimLegendaryEdition>
{
    private readonly TestModDownloader _downloader;
    private AVerbTest _verbTester;

    public SkyrimLegendaryEditionTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _downloader = serviceProvider.GetRequiredService<TestModDownloader>();
        _verbTester = new AVerbTest(serviceProvider.GetRequiredService<TemporaryFileManager>(), serviceProvider);
    }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public async Task CanInstallAndApplyMostPopularMods()
    {


        var sksePath = FileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("Assets/DownloadableMods/HasScriptExtender/skse_1_07_03.zip");
        const string skseModName = "skse_1_07_03";

        const int skyuiModId = 3863;
        const int skyuiFileId = 1000172397;
        const string skyuiModName = "skyui";

        const int uslep = 71214;
        const int uslepFileId = 1000306031;
        const string uslepModName = "Unofficial skyrim legendary edition patch";

        // manage the game
        // Note: can't create the loadout using CLI as it would index the game files,
        // and other tests might pollute the game folder in the meantime.
        var loadout = await CreateLoadout(indexGameFiles: false);
        var loadoutName = loadout.Value.Name;


        await _verbTester.RunNoBannerAsync("list-managed-games");

        _verbTester.LastTable.Columns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        _verbTester.LastTable.Rows.FirstOrDefault(r => r.First().Equals(loadoutName)).Should().NotBeNull();

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(1);

        // install SKSE
        await _verbTester.RunNoBannerAsync("install-mod", "-l", loadoutName, "-f", sksePath.ToString(), "-n", skseModName);

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(2);

        await _verbTester.RunNoBannerAsync("list-mod-contents", "-l", loadoutName, "-n", skseModName);
        _verbTester.LastTable.Rows.Count().Should().Be(127);

        // Test Apply
        await _verbTester.RunNoBannerAsync("flatten-list", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(127);

        await _verbTester.RunNoBannerAsync("apply", "-l", loadoutName, "-r", "false");
        _verbTester.LastTable.Rows.Count().Should().Be(127);

        // install skyui
        var uri = $"nxm://{Game.Domain}/mods/{skyuiModId}/files/{skyuiFileId}";
        await _verbTester.RunNoBannerAsync("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            skyuiModName);

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(3);

        await _verbTester.RunNoBannerAsync("list-mod-contents", "-l", loadoutName, "-n", skyuiModName);
        _verbTester.LastTable.Rows.Count().Should().Be(5);

        // install uslep
        uri = $"nxm://{Game.Domain}/mods/{uslep}/files/{uslepFileId}";
        await _verbTester.RunNoBannerAsync("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            uslepModName);

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(4);

        await _verbTester.RunNoBannerAsync("list-mod-contents", "-l", loadoutName, "-n", uslepModName);
        _verbTester.LastTable.Rows.Count().Should().Be(5);

        // Test Apply
        await _verbTester.RunNoBannerAsync("flatten-list", "-l", loadoutName);
        // count plugins.txt
        _verbTester.LastTable.Rows.Count().Should().Be(137);

        await _verbTester.RunNoBannerAsync("apply", "-l", loadoutName, "-r", "false");
        // depending on the state of plugins.txt, there could be more or less steps
        _verbTester.LastTable.Rows.Count().Should().Be(137);
    }
}
