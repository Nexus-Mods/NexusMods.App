using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests;

public class MountAndBlade2BannerlordTests
{
    private readonly ILogger<MountAndBlade2BannerlordTests> _logger;
    private readonly MountAndBlade2Bannerlord _game;
    private readonly LoadoutManager _manager;

    public MountAndBlade2BannerlordTests(ILogger<MountAndBlade2BannerlordTests> logger, MountAndBlade2Bannerlord game, LoadoutManager manager)
    {
        _logger = logger;
        _game = game;
        _manager = manager;
    }

    [Fact]
    public void CanFindGames()
    {
        _game.Name.Should().Be(MountAndBlade2Bannerlord.DisplayName);
        _game.Domain.Should().Be(MountAndBlade2Bannerlord.StaticDomain);
        _game.Installations.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CanLoadLoadout()
    {
        var loadout = await _manager.ImportFromAsync(KnownFolders.EntryFolder.CombineUnchecked(@"Resources\bannerlord_v1.0.3.9860.zip"));
        loadout.Value.Mods.Values.Select(m => m.Name).Should().Contain("Game Files");
        var gameFiles = loadout.Value.Mods.Values.First(m => m.Name == "Game Files");
        gameFiles.Files.Count.Should().BeGreaterThan(0);

        var native = gameFiles.Files.Values.First(f => f.To == new GamePath(GameFolderType.Game, "Modules/Native/SubModule.xml"));
        //native.Metadata.Should().ContainItemsAssignableTo<ModuleIdMetadata>();
        //
        //gameFiles.Files.Values.OfType<PluginFile>().Should().ContainSingle();
    }
}
