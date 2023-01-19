using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

[Trait("RequiresGameInstalls", "True")]
public class SkyrimSpecialEditionTests
{
    private readonly ILogger<SkyrimSpecialEditionTests> _logger;
    private readonly SkyrimSpecialEdition _game;
    private readonly LoadoutManager _manager;

    public SkyrimSpecialEditionTests(ILogger<SkyrimSpecialEditionTests> logger,
        SkyrimSpecialEdition game,
        LoadoutManager manager)
    {
        _logger = logger;
        _game = game;
        _manager = manager;
    }

    [Fact]
    public void CanFindGames()
    {
        _game.Name.Should().Be("Skyrim Special Edition");
        _game.Slug.Should().Be("skyrimspecialedition");
        _game.Installations.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CanCreateLoadout()
    {
        var loadout = await _manager.ManageGame(_game.Installations.First(), Guid.NewGuid().ToString());
        loadout.Value.Mods.Select(m => m.Name).Should().Contain("Game Files");
        var gameFiles = loadout.Value.Mods.First(m => m.Name == "Game Files");
        gameFiles.Files.Count.Should().BeGreaterThan(0);

        var dragonborn = gameFiles.Files.First(f => f.To == new GamePath(GameFolderType.Game, "Data/Dragonborn.esm"));
        dragonborn.Metadata.Should().ContainItemsAssignableTo<AnalysisSortData>();

        gameFiles.Files.OfType<PluginFile>().Should().ContainSingle();
    }

    [Fact]
    public async Task CanGeneratePluginsFile()
    {
        var loadout = await _manager.ManageGame(_game.Installations.First(), Guid.NewGuid().ToString());
        loadout.Value.Mods.Select(m => m.Name).Should().Contain("Game Files");
        var gameFiles = loadout.Value.Mods.First(m => m.Name == "Game Files");
        gameFiles.Files.Count.Should().BeGreaterThan(0);

        var pluginFile = gameFiles.Files.OfType<PluginFile>().First();

        var flattenedList = loadout.FlattenList().ToArray();

        using var ms = new MemoryStream();
        await pluginFile.GenerateAsync(ms, loadout.Value, flattenedList);

        ms.Position = 0;

        var (size, hash) = await pluginFile.GetMetaData(loadout.Value, flattenedList);

        size.Should().Be(ms.Length);
        (await ms.Hash()).Should().Be(hash);

        var results = Encoding.UTF8.GetString(ms.ToArray())
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        if (results.Length == 9)
        {
            // Skyrim SE without CC downloads
            results.Should()
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
        else {
            // Skyrim SE with CC downloads
            results.Should()
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
                }, opt => opt.WithStrictOrdering());
        }
    }
    
    [Fact]
    public async Task CanDeployLoadout()
    {
        var loadout = await _manager.ManageGame(_game.Installations.First(), Guid.NewGuid().ToString());
        await loadout.Apply();
    }
}