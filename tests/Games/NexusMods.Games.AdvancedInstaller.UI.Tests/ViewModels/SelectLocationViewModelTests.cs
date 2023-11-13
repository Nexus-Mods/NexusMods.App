using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ViewModels;

// public class SelectLocationViewModelTests
// {
//     [Theory, AutoFileSystem]
//     public void CreateSelectLocationViewModel(AbsolutePath gameDir, AbsolutePath savesDir)
//     {
//         // Arrange
//         const string rootName = "Skyrim Special Edition";
//         var fs = CreateInMemoryFs(gameDir);
//         fs.AddSavePaths(savesDir);
//         var register = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>()
//         {
//             { LocationId.Game, fs.FromUnsanitizedFullPath(gameDir.GetFullPath()) },
//             { LocationId.Saves, fs.FromUnsanitizedFullPath(savesDir.GetFullPath()) }
//         });
//
//         // Act
//         var vm = new SelectLocationViewModel(register, default!, rootName);
//         vm.TreeContainersVMs.Should().HaveCount(2);
//
//         // Assert Regular Tree
//         var game = GetTreeWithLocationId(vm, LocationId.Game);
//         game.Should().NotBeNull();
//         game!.DisplayName.Should().Be(rootName);
//
//         var save = GetTreeWithLocationId(vm, LocationId.Saves);
//         save.Should().NotBeNull();
//     }
//
//     private static ISelectableTreeEntryViewModel? GetTreeWithLocationId(SelectLocationViewModel vm, LocationId id)
//     {
//         ISelectableTreeEntryViewModel? tevm = null;
//         _ = vm.TreeContainersVMs.First(x =>
//         {
//             var success = x.Tree.TryGetModelAt(0, out tevm);
//             if (success)
//                 return tevm!.Path.LocationId == id;
//
//             return false;
//         });
//         return tevm;
//     }
// }
