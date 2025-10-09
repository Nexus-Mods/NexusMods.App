using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Games.IntegrationTests.CreationEngine.Tests;

public class BasicSkyrimTests : ASkyrimSETest
{
 
    [Test]
    public async Task CanListBaseGamePlugins()
    {
        var loadout = await CreateLoadout();

    }   
}
