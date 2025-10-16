using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.CreationEngine;
using NexusMods.Games.CreationEngine.SkyrimSE;
using NexusMods.Games.IntegrationTestFramework;

namespace NexusMods.Games.IntegrationTests.CreationEngine.Tests;

[SkyrimSESteamCurrent]
[Fallout4SteamCurrent]
public class BasicCreationEngineTests(Type game, GameLocatorResult locatorResult) : AGameIntegrationTest(game, locatorResult)
{
    [Test]
    public async Task GameFilesExistOnDisk()
    {
        var loadout = await CreateLoadout();
        
        var syncTree = await Synchronizer.BuildSyncTree(loadout);

        foreach (var (path, node) in syncTree)
        {
            if (!path.InFolder(SkyrimSEKnownPaths.AppDataPath)) 
                continue;
            
            // Get the plugins and archives from the Data folder
            if (!(KnownCEExtensions.PluginFiles.Contains(path.Extension) || path.Extension == KnownCEExtensions.BSA))
                continue;
            
            // Make sure the sync tree shows that we have the file on disk, and that the file exists on disk
            node.HaveDisk.Should().BeTrue();
            GameInstallation.LocationsRegister.GetResolvedPath(path).FileExists.Should().BeTrue();
        }
    }   
}
