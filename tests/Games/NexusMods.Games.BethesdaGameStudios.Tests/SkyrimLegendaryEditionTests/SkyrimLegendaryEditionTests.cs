using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.Tests.VerbTests;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimLegendaryEditionTests;

public class SkyrimLegendaryEditionTests(IServiceProvider serviceProvider) : AGameTest<SkyrimLegendaryEdition.SkyrimLegendaryEdition>(serviceProvider)
{
    private readonly TestModDownloader _downloader = serviceProvider.GetRequiredService<TestModDownloader>();
    private readonly AVerbTest _verbTester = new AVerbTest(serviceProvider);

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


        var log = await _verbTester.Run("list-loadouts");

        log.LastTableColumns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        log.TableCellsWith(loadoutName).Should().NotBeNull();

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(2);

        // install SKSE
        log = await _verbTester.Run("install-mod", "-l", loadoutName, "-f", sksePath.ToString(), "-n", skseModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(3);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", skseModName);
        log.LastTable.Rows.Count().Should().Be(127);

        // Test Apply
        log = await _verbTester.Run("flatten-loadout", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(128);

        log = await _verbTester.Run("apply", "-l", loadoutName);

        // install skyui
        var uri = $"nxm://{Game.Domain}/mods/{skyuiModId}/files/{skyuiFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            skyuiModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(4);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", skyuiModName);
        log.LastTable.Rows.Count().Should().Be(5);

        // install uslep
        uri = $"nxm://{Game.Domain}/mods/{uslep}/files/{uslepFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            uslepModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(5);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", uslepModName);
        log.LastTable.Rows.Count().Should().Be(5);

        // Test Apply
        log = await _verbTester.Run("flatten-loadout", "-l", loadoutName);
        // count plugins.txt
        log.LastTable.Rows.Count().Should().Be(138);

        log = await _verbTester.Run("apply", "-l", loadoutName);
    }
}
