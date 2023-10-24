using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.SelectableDirectoryVMTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ViewModels;

public class SelectLocationViewModelTests
{
    [Theory, AutoFileSystem]
    public void CreateSelectLocationViewModel(AbsolutePath gameDir, AbsolutePath savesDir)
    {
        // Arrange
        const string rootName = "Skyrim Special Edition";
        var fs = CreateInMemoryFs(gameDir);
        fs.AddSavePaths(savesDir);
        var register = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, gameDir },
            { LocationId.Saves, savesDir }
        });

        // Act
        var vm = new SelectLocationViewModel(register, rootName);
        vm.AllFoldersTrees.Should().HaveCount(2);

        // Assert Regular Tree
        var game = GetTreeWithLocationId(vm, LocationId.Game);
        game.Should().NotBeNull();
        game!.DisplayName.Should().Be(rootName);

        var save = GetTreeWithLocationId(vm, LocationId.Saves);
        save.Should().NotBeNull();
    }

    private static ITreeEntryViewModel? GetTreeWithLocationId(SelectLocationViewModel vm, LocationId id)
    {
        ITreeEntryViewModel? tevm = null;
        _ = vm.AllFoldersTrees.First(x =>
        {
            var success = x.Tree.TryGetModelAt(0, out tevm);
            if (success)
                return tevm!.Path.LocationId == id;

            return false;
        });
        return tevm;
    }
}
