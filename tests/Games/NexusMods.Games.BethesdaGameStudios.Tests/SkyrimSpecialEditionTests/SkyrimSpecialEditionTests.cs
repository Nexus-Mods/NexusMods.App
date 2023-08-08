using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.CLI.Tests.VerbTests;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using Noggog;

namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimSpecialEditionTests;

public class SkyrimSpecialEditionTests : AGameTest<SkyrimSpecialEdition>
{
    private readonly TestModDownloader _downloader;
    private AVerbTest _verbTester;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public SkyrimSpecialEditionTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _downloader = serviceProvider.GetRequiredService<TestModDownloader>();
        _verbTester = new AVerbTest(serviceProvider.GetRequiredService<TemporaryFileManager>(), serviceProvider);
    }

    [Fact]
    public void CanFindGames()
    {
        Game.Name.Should().Be("Skyrim Special Edition");
        Game.Domain.Should().Be(SkyrimSpecialEdition.StaticDomain);
        Game.Installations.Count().Should().BeGreaterThan(0);
    }

    [Fact]
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

        await _verbTester.RunNoBannerAsync("list-managed-games");

        _verbTester.LastTable.Columns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        _verbTester.LastTable.Rows.FirstOrDefault(r => r.First().Equals(loadoutName)).Should().NotBeNull();

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(1);

        // install skse
        var uri = $"nxm://{Game.Domain}/mods/{skseModId}/files/{skseFileId}";
        await _verbTester.RunNoBannerAsync("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n", skseModName);

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(2);

        await _verbTester.RunNoBannerAsync("list-mod-contents", "-l", loadoutName, "-n", skseModName);
        _verbTester.LastTable.Rows.Count().Should().Be(128);

        // install skyui
        uri = $"nxm://{Game.Domain}/mods/{skyuiModId}/files/{skyuiFileId}";
        await _verbTester.RunNoBannerAsync("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            skyuiModName);

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(3);

        await _verbTester.RunNoBannerAsync("list-mod-contents", "-l", loadoutName, "-n", skyuiModName);
        _verbTester.LastTable.Rows.Count().Should().Be(6);

        // install ussep
        uri = $"nxm://{Game.Domain}/mods/{ussepModId}/files/{ussepFileId}";
        await _verbTester.RunNoBannerAsync("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            ussepModName);

        await _verbTester.RunNoBannerAsync("list-mods", "-l", loadoutName);
        _verbTester.LastTable.Rows.Count().Should().Be(4);

        await _verbTester.RunNoBannerAsync("list-mod-contents", "-l", loadoutName, "-n", ussepModName);
        _verbTester.LastTable.Rows.Count().Should().Be(8);

        // Test Apply
        await _verbTester.RunNoBannerAsync("flatten-list", "-l", loadoutName);
        // count plugins.txt
        var logger = ServiceProvider.GetRequiredService<ILogger<SkyrimSpecialEditionTests>>();
        StringBuilder sb = new();
        _verbTester.LastTable.Rows.ForEach(r =>
        {
            r.ForEach(c => sb.Append(c.ToString() + ","));
            sb.AppendLine();
        });
        logger.LogInformation("flatten-list table {FlattenTable}", sb.ToString());
        _verbTester.LastTable.Rows.Count().Should().Be(143);

        await _verbTester.RunNoBannerAsync("apply", "-l", loadoutName, "-r", "false");
        // depending on the state of plugins.txt, there could be more or less steps
        _verbTester.LastTable.Rows.Count().Should().BeGreaterThan(142);
    }

    [Fact]
    public async Task CanGeneratePluginsFile()
    {
        var loadout = await CreateLoadout(indexGameFiles: false);

        var analysisStr = await BethesdaTestHelpers.GetAssetsPath(FileSystem).Combine("plugin_dependencies.json")
            .ReadAllTextAsync();
        var analysis = JsonSerializer.Deserialize<Dictionary<string, string[]>>(analysisStr)!;


        var gameFiles =
            loadout.Value.Mods.Values.First(m => m.ModCategory == Mod.GameFilesCategory); // <= throws on failure

        LoadoutRegistry.Alter(loadout.Value.LoadoutId, gameFiles.Id, "Added plugins", mod =>
        {
            var files = mod!.Files;
            foreach (var file in analysis)
            {
                var newFile = new GameFile()
                {
                    Id = ModFileId.New(),
                    Installation = loadout.Value.Installation,
                    To = new GamePath(GameFolderType.Game, $"Data/{file.Key}"),
                    Hash = Hash.Zero,
                    Size = Size.Zero,
                    Metadata =
                        ImmutableList<IMetadata>.Empty.Add(
                            new PluginAnalysisData
                            {
                                Masters = file.Value.Select(f => f.ToRelativePath()).ToArray()
                            })
                };
                files = files.With(newFile.Id, newFile);
            }

            return mod with { Files = files };
        });


        gameFiles.Files.Count.Should().BeGreaterThan(0);

        LoadoutRegistry.Get(loadout.Value.LoadoutId, gameFiles.Id)!.Files.Values
            .Count(x => x.Metadata.OfType<PluginAnalysisData>().Any())
            .Should().BeGreaterOrEqualTo(analysis.Count, "Analysis data has been added");

        var pluginOrderFile = gameFiles.Files.Values.OfType<PluginOrderFile>().First();
        var flattenedList = (await LoadoutSynchronizer.FlattenLoadout(loadout.Value)).Files.Values.ToList();

        var plan = await LoadoutSynchronizer.MakeApplySteps(loadout.Value);

        using var ms = new MemoryStream();
        await pluginOrderFile.GenerateAsync(ms, plan);

        ms.Position = 0;

        (await ms.XxHash64Async()).Should().Be(Hash.From(0x68B821EEFD98523C));
        var results = Encoding.UTF8.GetString(ms.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        // CC = Creation Club
        if (results.Length == 9)
        {
            // Skyrim SE without CC downloads
            results.Select(t => t.TrimStart('*')).Should()
                .BeEquivalentTo(new[]
                {
                    "Skyrim.esm",
                    "Update.esm",
                    "Dawnguard.esm",
                    "HearthFires.esm",
                    "Dragonborn.esm",
                    "ccBGSSSE001-Fish.esm",
                    "ccBGSSSE025-AdvDSGS.esm",
                    "ccBGSSSE037-Curios.esl",
                    "ccQDRSSE001-SurvivalMode.esl"
                });
        }
        else
        {
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
                        "ccvsvsse004-beafarmer.esl"
                    }.Select(t => t.ToLowerInvariant()),
                    opt => opt.WithStrictOrdering());
        }
    }

    [Fact]
    public async Task EnablingAndDisablingModsModifiesThePluginsFile()
    {
        var loadout = await CreateLoadout(indexGameFiles: false);

        var pluginFile = (from mod in loadout.Value.Mods.Values
                from file in mod.Files.Values
                where file is PluginOrderFile
                select file)
            .OfType<PluginOrderFile>()
            .First();


        var pluginFilePath = pluginFile.To.CombineChecked(loadout.Value.Installation);

        var path = BethesdaTestHelpers.GetDownloadableModFolder(FileSystem, "SkyrimBase");
        var downloaded = await _downloader.DownloadFromManifestAsync(path, FileSystem);

        var skyrimBase = await InstallModFromArchiveIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        await Apply(loadout.Value);

        pluginFilePath.FileExists.Should().BeTrue("the loadout is applied");


        var text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().NotContain("plugin_test.esp", "plugin_test.esp is not installed");

        path = BethesdaTestHelpers.GetDownloadableModFolder(FileSystem, "PluginTest");
        downloaded = await _downloader.DownloadFromManifestAsync(path, FileSystem);
        var pluginTest = await InstallModFromArchiveIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        await Apply(loadout.Value);

        pluginFilePath.FileExists.Should().BeTrue("the loadout is applied");
        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "plugin_test.esp is installed");

        LoadoutRegistry.Alter(loadout.Value.LoadoutId, pluginTest.Id, "disable plugin",
            mod => mod with { Enabled = false });

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "new loadout has not been applied yet");

        await Apply(loadout.Value);

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().NotContain("plugin_test.esp", "plugin_test.esp is disabled");

        LoadoutRegistry.Alter(loadout.Value.LoadoutId, pluginTest.Id, "enable plugin",
            mod => mod with { Enabled = true });

        await Apply(loadout.Value);

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
