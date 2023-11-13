using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ViewModels;

// public class BodyViewModelTests
// {
//     [Theory, AutoFileSystem]
//     public void When_MultiSelecting_ShowsCorrectMenuAndKeepsTrackOfItems(AbsolutePath gameDir, AbsolutePath savesDir)
//     {
//         // Arrange, Act & Assert
//         var bodyVm = CommonSetup(gameDir, savesDir);
//
//         // Add one folder
//         var meshes = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Meshes");
//         bodyVm.OnSelect(meshes);
//         bodyVm.SelectedItems.Count.Should().Be(1);
//         bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.SelectLocationViewModel);
//
//         // Add another
//         var textures = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Textures");
//         bodyVm.OnSelect(textures);
//         bodyVm.SelectedItems.Count.Should().Be(2);
//         bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.SelectLocationViewModel);
//
//         // Remove a folder
//         bodyVm.OnCancelSelect(meshes);
//         bodyVm.SelectedItems.Count.Should().Be(1);
//         bodyVm.SelectedItems.First().Should().Be(textures);
//         bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.SelectLocationViewModel);
//
//         // Remove last folder
//         bodyVm.OnCancelSelect(textures);
//         bodyVm.SelectedItems.Count.Should().Be(0);
//         bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.EmptyPreviewViewModel);
//     }
//
//     [Theory, AutoFileSystem]
//     public void When_SelectingATarget_PerformsLinking(AbsolutePath gameDir, AbsolutePath savesDir)
//     {
//         // Arrange, Act & Assert
//         var bodyVm = CommonSetup(gameDir, savesDir);
//         var gameRoot = GetTreeForLocationId(bodyVm.SelectLocationViewModel, LocationId.Game).Root;
//
//         // Add one folder
//         var meshes = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Meshes");
//         (meshes as ModContentTreeEntryViewModel<ModSourceFileEntry>)?.BeginSelect();
//         // Since UI isn't actually activated all the connections don't work, so we have to manually call the method
//         bodyVm.OnSelect(meshes);
//
//
//         var gameData = gameRoot.GetChild("Data");
//         bodyVm.OnDirectorySelected(gameData);
//
//         // Assert everything went smooth
//         var data = bodyVm.Data;
//
//         // Link Data
//         data.ArchiveToOutputMap["Meshes/greenBlade.nif"].Should()
//             .Be(new GamePath(LocationId.Game, "Data/Meshes/greenBlade.nif"));
//
//         // Source Directory Data
//         meshes.Status.Should().Be(OldModContentNodeStatus.IncludedExplicit);
//
//         // Preview Data
//         var previewData = GetTreeForLocationId(bodyVm.PreviewViewModel, LocationId.Game).Root;
//         previewData.GetNode("Data")!.GetNode("Meshes")!.GetNode("greenBlade.nif").Should().NotBeNull();
//
//         // Shown in preview
//         bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.PreviewViewModel);
//     }
//
//     [Theory, AutoFileSystem]
//     public void When_SelectingMultipleTargets_PerformsLinking(AbsolutePath gameDir, AbsolutePath savesDir)
//     {
//         // Arrange, Act & Assert
//         var bodyVm = CommonSetup(gameDir, savesDir);
//         var gameRoot = GetTreeForLocationId(bodyVm.SelectLocationViewModel, LocationId.Game).Root;
//
//         // Add one folder
//         var meshes = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Meshes");
//         (meshes as ModContentTreeEntryViewModel<ModSourceFileEntry>)?.BeginSelect();
//         bodyVm.OnSelect(meshes);
//
//         var textures = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Textures");
//         (textures as ModContentTreeEntryViewModel<ModSourceFileEntry>)?.BeginSelect();
//         bodyVm.OnSelect(textures);
//
//         // Bind inside of Meshes and Textures to data (this is invalid for Skyrim, but for test is okay)
//         var gameMeshes = gameRoot.GetChild("Data");
//         bodyVm.OnDirectorySelected(gameMeshes);
//
//         // Assert everything went smooth
//         var data = bodyVm.Data;
//
//         // Link Data
//         data.ArchiveToOutputMap["Meshes/greenBlade.nif"].Should()
//             .Be(new GamePath(LocationId.Game, "Data/Meshes/greenBlade.nif"));
//         data.ArchiveToOutputMap["Textures/greenBlade.dds"].Should()
//             .Be(new GamePath(LocationId.Game, "Data/Textures/greenBlade.dds"));
//
//         // Source Directory Data
//         meshes.Status.Should().Be(OldModContentNodeStatus.IncludedExplicit);
//         textures.Status.Should().Be(OldModContentNodeStatus.IncludedExplicit);
//
//         // Preview Data
//         var previewData = GetTreeForLocationId(bodyVm.PreviewViewModel, LocationId.Game).Root;
//         previewData.GetNode("Data")!.GetNode("Meshes")!.GetNode("greenBlade.nif").Should().NotBeNull();
//         previewData.GetNode("Data")!.GetNode("Textures")!.GetNode("greenBlade.dds").Should().NotBeNull();
//
//         // Shown in preview
//         bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.PreviewViewModel);
//     }
//
//     private BodyViewModel CommonSetup(AbsolutePath gameDir, AbsolutePath savesDir)
//     {
//         var fs = CreateInMemoryFs(gameDir);
//         fs.AddPaths(gameDir, GetGameFolderPaths());
//         fs.AddSavePaths(savesDir);
//         var register = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>()
//         {
//             { LocationId.Game, fs.FromUnsanitizedFullPath(gameDir.GetFullPath()) },
//             { LocationId.Saves, fs.FromUnsanitizedFullPath(savesDir.GetFullPath()) }
//         });
//
//         return new BodyViewModel("Test Mod", ModContentVMTestHelpers.CreateTestTreeMSFE(), register);
//     }
//
//     public static ILocationTreeContainerViewModel GetTreeForLocationId(ISelectLocationViewModel vm, LocationId id) =>
//         vm.TreeContainers.First(x => x.Root.Path.LocationId == id);
//
//     private static ILocationPreviewTreeViewModel GetTreeForLocationId(IPreviewViewModel vm, LocationId id) =>
//         vm.TreeContainers.First(x => x.Root.FullPath.LocationId == id);
// }
