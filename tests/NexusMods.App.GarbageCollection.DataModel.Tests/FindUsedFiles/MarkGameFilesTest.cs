using NexusMods.Abstractions.Library;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.App.GarbageCollection.DataModel.Tests.FindUsedFiles;

/// <summary>
/// This ensures that 'game files' are marked as roots when performing the GC action.
/// That is, the backup of game data which we made.
/// </summary>
/// <param name="serviceProvider"></param>
/// <param name="libraryService"></param>
public class MarkGameFilesTest(IServiceProvider serviceProvider, ILibraryService libraryService) : AGameTest<StubbedGame>(serviceProvider)
{
    [Fact]
    public async Task ShouldVerifyGameFilesAreRooted()
    {
        // Setup: Manage a game and make a 'vanilla' loadout.
        // The synchronizer marks the game files as roots.
        var loadout = await CreateLoadout();

        // Act: Run a GC.
        
        // Assert: No game files should be deleted from FileStore, they are roots.
        
    }
}
