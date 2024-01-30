using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.CLI.Tests.VerbTests;
using NexusMods.DataModel.Loadouts.Extensions;
using NexusMods.DataModel.LoadoutSynchronizer.Extensions;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Noggog;

namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimSpecialEditionTests;

public class SkyrimSpecialEditionTests : AGameTest<SkyrimSpecialEdition.SkyrimSpecialEdition>
{
    private readonly TestModDownloader _downloader;
    private AVerbTest _verbTester;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public SkyrimSpecialEditionTests(TestModDownloader downloader, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _downloader = downloader;
        _verbTester = new AVerbTest(serviceProvider);
    }

    [Fact]
    public void CanFindGames()
    {
        Game.Name.Should().Be("Skyrim Special Edition");
        Game.Domain.Should().Be(SkyrimSpecialEdition.SkyrimSpecialEdition.StaticDomain);
        Game.Installations.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public async Task CanInstallAndApplyMostPopularMods()
    {
        const int skseModId = 30379;
        const int skseFileId = 323365;
        const string skseModName = "skse64";

        const int skyuiModId = 12604;
        const int skyuiFileId = 35407;
        const string skyuiModName = "skyui";

        const int ussepModId = 266;
        const int ussepFileId = 392477;
        const string ussepModName = "Unofficial skyrim special edition patch";

        // manage the game
        // Note: can't create the loadout using CLI as it would index the game files,
        // and other tests might pollute the game folder in the meantime.
        var loadout = await CreateLoadout(indexGameFiles: false);
        var loadoutName = loadout.Value.Name;

        var modPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Assets/TruncatedPlugins.7z");
        await InstallModStoredFileIntoLoadout(loadout, modPath, "Skyrim Truncated Plugins");

        var log = await _verbTester.Run("list-loadouts");

        log.LastTableColumns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        log.TableCellsWith(loadoutName).Should().NotBeEmpty();

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(3);

        // install skse
        var uri = $"nxm://{Game.Domain}/mods/{skseModId}/files/{skseFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n", skseModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(4);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", skseModName);
        log.LastTable.Rows.Count().Should().Be(128);

        // install skyui
        uri = $"nxm://{Game.Domain}/mods/{skyuiModId}/files/{skyuiFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            skyuiModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(5);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", skyuiModName);
        log.LastTable.Rows.Count().Should().Be(6);

        // install ussep
        uri = $"nxm://{Game.Domain}/mods/{ussepModId}/files/{ussepFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            ussepModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(6);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", ussepModName);
        log.LastTable.Rows.Count().Should().Be(8);

        // Test Apply
        log = await _verbTester.Run("flatten-loadout", "-l", loadoutName);
        // count plugins.txt
        var logger = ServiceProvider.GetRequiredService<ILogger<SkyrimSpecialEditionTests>>();
        StringBuilder sb = new();
        log.LastTable.Rows.ForEach(r =>
        {
            r.ForEach(c => sb.Append(c.ToString() + ","));
            sb.AppendLine();
        });
        logger.LogInformation("flatten-list table {FlattenTable}", sb.ToString());
        log.LastTable.Rows.Count().Should().Be(223);

        log = await _verbTester.Run("apply", "-l", loadoutName);
    }

    [Fact]
    public async Task CanGeneratePluginsFile()
    {
        var loadout = await CreateLoadout(indexGameFiles: false);
        var mod = await InstallTruncatedPlugins(loadout);

        var analysisStr = await BethesdaTestHelpers.GetAssetsPath(FileSystem).Combine("plugin_dependencies.json")
            .ReadAllTextAsync();
        var analysis = JsonSerializer.Deserialize<Dictionary<string, string[]>>(analysisStr)!;


        var metadataFiles =
            loadout.Value.Mods.Values.First(m => m.ModCategory == Abstractions.DataModel.Entities.Mods.Mod.ModdingMetaData); // <= throws on failure

        var gameFiles =
            loadout.Value.Mods.Values.First(m => m.ModCategory == Abstractions.DataModel.Entities.Mods.Mod.GameFilesCategory); // <= throws on failure

        var modPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Assets/SMIM_Truncated_Plugins.7z");
        await InstallModStoredFileIntoLoadout(loadout, modPath, "SMIM");

        var pluginOrderFile = metadataFiles.Files.Values.OfType<PluginOrderFile>().First();

        var flattened = await loadout.Value.ToFlattenedLoadout();

        await Task.Delay(100);
        using var ms = new MemoryStream();
        await pluginOrderFile.Write(ms, loadout.Value, flattened, await loadout.Value.ToFileTree());
        await ms.FlushAsync();


        flattened.GetAllDescendentFiles()
            .ToArray()
            .Length
            .Should()
            .Be(83, "the loadout has all the files");

        ms.Position = 0;
        var results = Encoding.UTF8.GetString(ms.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        //(await ms.XxHash64Async()).Should().Be(Hash.From(0xEF46DB3751D8E999));


        // Skyrim SE with CC downloads
        results
            .Select(t => t.TrimStart('*').ToLowerInvariant())
            .Should()
            .BeEquivalentTo(new[]
                {
                    "Skyrim.esm",
                    "Update.esm",
                    "Dawnguard.esm",
                    "HearthFires.esm",
                    "Dragonborn.esm",
                    "ccafdsse001-dwesanctuary.esm",
                    "ccasvsse001-almsivi.esm",
                    "ccbgssse001-fish.esm",
                    "ccbgssse016-umbra.esm",
                    "ccbgssse025-advdsgs.esm",
                    "ccbgssse031-advcyrus.esm",
                    "ccbgssse067-daedinv.esm",
                    "cceejsse001-hstead.esm",
                    "cceejsse005-cave.esm",
                    "cctwbsse001-puzzledungeon.esm",
                    "ccbgssse002-exoticarrows.esl",
                    "ccbgssse003-zombies.esl",
                    "ccbgssse004-ruinsedge.esl",
                    "ccbgssse005-goldbrand.esl",
                    "ccbgssse006-stendarshammer.esl",
                    "ccbgssse007-chrysamere.esl",
                    "ccbgssse008-wraithguard.esl",
                    "ccbgssse010-petdwarvenarmoredmudcrab.esl",
                    "ccbgssse011-hrsarmrelvn.esl",
                    "ccbgssse012-hrsarmrstl.esl",
                    "ccbgssse013-dawnfang.esl",
                    "ccbgssse014-spellpack01.esl",
                    "ccbgssse018-shadowrend.esl",
                    "ccbgssse019-staffofsheogorath.esl",
                    "ccbgssse020-graycowl.esl",
                    "ccbgssse021-lordsmail.esl",
                    "ccbgssse034-mntuni.esl",
                    "ccbgssse035-petnhound.esl",
                    "ccbgssse036-petbwolf.esl",
                    "ccbgssse037-curios.esl",
                    "ccbgssse038-bowofshadows.esl",
                    "ccbgssse040-advobgobs.esl",
                    "ccbgssse041-netchleather.esl",
                    "ccbgssse043-crosselv.esl",
                    "ccbgssse045-hasedoki.esl",
                    "ccbgssse050-ba_daedric.esl",
                    "ccbgssse051-ba_daedricmail.esl",
                    "ccbgssse052-ba_iron.esl",
                    "ccbgssse053-ba_leather.esl",
                    "ccbgssse054-ba_orcish.esl",
                    "ccbgssse055-ba_orcishscaled.esl",
                    "ccbgssse056-ba_silver.esl",
                    "ccbgssse057-ba_stalhrim.esl",
                    "ccbgssse058-ba_steel.esl",
                    "ccbgssse059-ba_dragonplate.esl",
                    "ccbgssse060-ba_dragonscale.esl",
                    "ccbgssse061-ba_dwarven.esl",
                    "ccbgssse062-ba_dwarvenmail.esl",
                    "ccbgssse063-ba_ebony.esl",
                    "ccbgssse064-ba_elven.esl",
                    "ccbgssse066-staves.esl",
                    "ccbgssse068-bloodfall.esl",
                    "ccbgssse069-contest.esl",
                    "cccbhsse001-gaunt.esl",
                    "ccedhsse001-norjewel.esl",
                    "ccedhsse002-splkntset.esl",
                    "ccedhsse003-redguard.esl",
                    "cceejsse002-tower.esl",
                    "cceejsse003-hollow.esl",
                    "cceejsse004-hall.esl",
                    "ccffbsse001-imperialdragon.esl",
                    "ccffbsse002-crossbowpack.esl",
                    "ccfsvsse001-backpacks.esl",
                    "cckrtsse001_altar.esl",
                    "ccmtysse001-knightsofthenine.esl",
                    "ccmtysse002-ve.esl",
                    "ccpewsse002-armsofchaos.esl",
                    "ccqdrsse001-survivalmode.esl",
                    "ccqdrsse002-firewood.esl",
                    "ccrmssse001-necrohouse.esl",
                    "ccvsvsse001-winter.esl",
                    "ccvsvsse002-pets.esl",
                    "ccvsvsse003-necroarts.esl",
                    "ccvsvsse004-beafarmer.esl",
                    "jks skyrim.esp",
                    "skyui_se.esp",
                    "smim-merged-all.esp",
                }.Select(t => t.ToLowerInvariant()),
                opt => opt.WithStrictOrdering());
    }

    /// <summary>
    /// Installs the test mod that contains around 80 truncated plugins. These are the game plugins
    /// and a few mods, but they have all been stripped of everything but their headers. So this is purely
    /// the metadata of the plugins, which is all we need to test the plugin order file generation.
    /// </summary>
    /// <param name="loadout"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<Abstractions.DataModel.Entities.Mods.Mod> InstallTruncatedPlugins(LoadoutMarker loadout)
    {
        var path = FileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("Assets/TruncatedPlugins.7z");
        var mod = await InstallModStoredFileIntoLoadout(loadout, path, "TruncatedPlugins");
        return mod;
    }

    [Fact]
    public async Task EnablingAndDisablingModsModifiesThePluginsFile()
    {
        var loadout = await CreateLoadout(indexGameFiles: false);

        loadout.Value.Mods.Values.SelectMany(m => m.Files.Values)
            .OfType<IToFile>()
            .Where(t => t.To.FileName == "plugin_test.esp")
            .Should()
            .BeEmpty("the mod is not installed");

        var pluginFile = (from mod in loadout.Value.Mods.Values
                from file in mod.Files.Values
                where file is PluginOrderFile
                select file)
            .OfType<PluginOrderFile>()
            .First();


        var pluginFilePath = pluginFile.To.CombineChecked(loadout.Value.Installation);

        var path = BethesdaTestHelpers.GetDownloadableModFolder(FileSystem, "SkyrimBase");
        var downloaded = await _downloader.DownloadFromManifestAsync(path, FileSystem);

        var skyrimBase = await InstallModStoredFileIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        await loadout.Value.Apply();

        pluginFilePath.FileExists.Should().BeTrue("the loadout is applied");


        var text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().NotContain("plugin_test.esp", "plugin_test.esp is not installed");

        path = BethesdaTestHelpers.GetDownloadableModFolder(FileSystem, "PluginTest");
        downloaded = await _downloader.DownloadFromManifestAsync(path, FileSystem);
        var pluginTest = await InstallModStoredFileIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        await loadout.Value.Apply();

        pluginFilePath.FileExists.Should().BeTrue("the loadout is applied");
        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "plugin_test.esp is installed");

        LoadoutRegistry.Alter(loadout.Value.LoadoutId, pluginTest.Id, "disable plugin",
            mod =>
            {
                mod.Should().NotBeNull();
                return mod! with { Enabled = false };
            });

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "new loadout has not been applied yet");

        await loadout.Value.Apply();

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().NotContain("plugin_test.esp", "plugin_test.esp is disabled");

        LoadoutRegistry.Alter(loadout.Value.LoadoutId, pluginTest.Id, "enable plugin",
            mod =>
            {
                mod.Should().NotBeNull();
                return mod! with { Enabled = true };
            });

        await loadout.Value.Apply();

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "plugin_test.esp is enabled again");
    }

    private static async Task<string[]> GetPluginOrder(AbsolutePath pluginFilePath)
    {
        return (await pluginFilePath.ReadAllTextAsync())
            .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.TrimStart("*"))
            .ToArray();
    }
}
