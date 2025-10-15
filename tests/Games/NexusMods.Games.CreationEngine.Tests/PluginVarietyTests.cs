using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.CreationEngine.LoadOrder;
using NexusMods.Games.CreationEngine.Tests.TestAttributes;
using NexusMods.Games.IntegrationTestFramework;

namespace NexusMods.Games.CreationEngine.Tests;

[Fallout4SteamCurrent]
[SkyrimSESteamCurrent]
public class PluginVarietyTests(Type gameType, GameLocatorResult locatorResult) : AGameIntegrationTest(gameType, locatorResult)
{
    [Test]
    public async Task GameSupportsPluginsVariety()
    {
        var loadout = await CreateLoadout();
        var plugins = Game.SortOrderManager.GetSortOrderVarieties()
            .OfType<PluginLoadOrderVariety>()
            .Single();
    }
}
