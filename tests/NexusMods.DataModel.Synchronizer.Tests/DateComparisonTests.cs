using FluentAssertions;
using FluentAssertions.Common;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class DateComparisonTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<GeneralModManagementTests>(helper)
{
    [Fact]
    public async Task DatesRoundTripCorrectlyThroughStorage()
    {
        
        // Setup the game files 
        await Synchronizer.RescanFiles(GameInstallation);
        var loadoutA = await CreateLoadout();
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        
        var pathToTest = new GamePath(LocationId.Game, "bin/modedFile.txt");
        var resolvedPath = GameInstallation.LocationsRegister.GetResolvedPath(pathToTest);
        resolvedPath.Parent.CreateDirectory();
        await resolvedPath.WriteAllTextAsync("Hello World!");
        
        await Synchronizer.RescanFiles(GameInstallation);
        loadoutA = await Synchronizer.Synchronize(loadoutA);
        
        resolvedPath.FileExists.Should().BeTrue("The file shouldn't be deleted via the sync process");

        var diskEntry = loadoutA.Installation.DiskStateEntries
            .First(f => f.Path.Item2 == pathToTest.LocationId && f.Path.Item3 == pathToTest.Path);
        
        // Sanity check to make sure dates are exactly as expected once they are ingested
        diskEntry.LastModified.Should().Be(resolvedPath.FileInfo.LastWriteTimeUtc);
    }
    
}
