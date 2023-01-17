using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Loadouts;
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
}