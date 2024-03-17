using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.Tests.VerbTests;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.BethesdaGameStudios.Tests.Fallout4Tests;

public class Fallout4Tests(IServiceProvider serviceProvider) : AGameTest<Fallout4.Fallout4>(serviceProvider)
{
    private readonly TestModDownloader _downloader = serviceProvider.GetRequiredService<TestModDownloader>();
    private readonly AVerbTest _verbTester = new AVerbTest(serviceProvider);

    [Fact]
    [Trait("FlakeyTest", "True")]
    public async Task CanInstallAndApplyMostPopularMods()
    {
        // ReSharper disable InconsistentNaming
        const int f4seModId = 42147, f4seFileId = 253313;
        const string f4seModName = "f4se";
        
        const int ufo4pModId = 4598, ufo4pFileId = 270951;
        const string ufo4pModName = "Unofficial fallout 4 patch";
        
        // manage the game
        // Note: can't create the loadout using CLI as it would index the game files,
        // and other tests might pollute the game folder in the meantime.
        var loadout = await CreateLoadout(indexGameFiles: false);
        var loadoutName = loadout.Value.Name;
        
        var log = await _verbTester.Run("list-loadouts");
        
        log.LastTableColumns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        log.TableCellsWith(loadoutName).Should().NotBeEmpty();
        
        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Should().HaveCount(2);

        // install f4se
        var uri = $"nxm://{Game.Domain}/mods/{f4seModId}/files/{f4seFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n", f4seModName);
        
        // log = await _verbTester.Run("list-mods", "-l", loadoutName);
        // log.LastTable.Rows.Should().HaveCount(?);
        //
        // log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-n", f4seModName);
        // log.LastTable.Rows.Should().HaveCount(?);
        //
        // // install ufo4p
        // uri = $"nxm://{Game.Domain}/mods/{ufo4pModId}/files/{ufo4pFileId}";
        // log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n", ufo4pModName);
        //
        // log = await _verbTester.Run("list-mods", "-l", loadoutName);
        // log.LastTable.Rows.Should().HaveCount(?);
        //
        // log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-n", ufo4pModName);
        // log.LastTable.Rows.Should().HaveCount(?);
        //
        // // Test Apply
        // log = await _verbTester.Run("flatten-list", "-l", loadoutName);
        // // count plugins.txt
        // log.LastTable.Rows.Should().HaveCount(?);
        //
        // log = await _verbTester.Run("apply", "-l", loadoutName, "-r", "false");
    }
}
