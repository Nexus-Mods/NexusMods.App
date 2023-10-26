using FluentAssertions;
using NexusMods.DataModel.Games;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.SelectableDirectoryVMTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ViewModels;

public class BodyViewModelTests
{
    [Theory, AutoFileSystem]
    public void When_MultiSelecting_ShowsCorrectMenuAndKeepsTrackOfItems(AbsolutePath gameDir, AbsolutePath savesDir)
    {
        // Arrange, Act & Assert
        var bodyVm = CommonSetup(gameDir, savesDir);

        // Add one folder
        var meshes = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Meshes");
        bodyVm.OnSelect(meshes);
        bodyVm.SelectedItems.Count.Should().Be(1);
        bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.SelectLocationViewModel);

        // Add another
        var textures = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Textures");
        bodyVm.OnSelect(textures);
        bodyVm.SelectedItems.Count.Should().Be(2);
        bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.SelectLocationViewModel);

        // Remove a folder
        bodyVm.OnCancelSelect(meshes);
        bodyVm.SelectedItems.Count.Should().Be(1);
        bodyVm.SelectedItems.First().Should().Be(textures);
        bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.SelectLocationViewModel);

        // Remove last folder
        bodyVm.OnCancelSelect(textures);
        bodyVm.SelectedItems.Count.Should().Be(0);
        bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.EmptyPreviewViewModel);
    }

    [Theory, AutoFileSystem]
    public void When_SelectingATarget_PerformsLinking(AbsolutePath gameDir, AbsolutePath savesDir)
    {
        // Arrange, Act & Assert
        var bodyVm = CommonSetup(gameDir, savesDir);
        var gameRoot = GetTreeForLocationId(bodyVm.SelectLocationViewModel, LocationId.Game).Root;

        // Add one folder
        var meshes = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Meshes");
        bodyVm.OnSelect(meshes);

        var gameMeshes = gameRoot.GetChild("Data").GetChild("Meshes");
        bodyVm.OnDirectorySelected(gameMeshes);

        // Assert everything went smooth
        var data = bodyVm.Data;

        // Link Data
        data.ArchiveToOutputMap["Meshes/greenBlade.nif"].Should()
            .Be(new GamePath(LocationId.Game, "Data/Meshes/greenBlade.nif"));

        // Source Directory Data
        meshes.Status.Should().Be(ModContentNodeStatus.IncludedExplicit);

        // Preview Data
        var previewData = GetTreeForLocationId(bodyVm.PreviewViewModel, LocationId.Game).Root;
        previewData.GetNode("Data")!.GetNode("Meshes")!.GetNode("greenBlade.nif").Should().NotBeNull();

        // Shown in preview
        bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.PreviewViewModel);
    }

    [Theory, AutoFileSystem]
    public void When_SelectingMultipleTargets_PerformsLinking(AbsolutePath gameDir, AbsolutePath savesDir)
    {
        // Arrange, Act & Assert
        var bodyVm = CommonSetup(gameDir, savesDir);
        var gameRoot = GetTreeForLocationId(bodyVm.SelectLocationViewModel, LocationId.Game).Root;

        // Add one folder
        var meshes = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Meshes");
        bodyVm.OnSelect(meshes);
        var textures = bodyVm.ModContentViewModel.Root.Children.First(x => x.FileName == "Textures");
        bodyVm.OnSelect(textures);

        // Bind inside of Meshes and Textures to data (this is invalid for Skyrim, but for test is okay)
        var gameMeshes = gameRoot.GetChild("Data");
        bodyVm.OnDirectorySelected(gameMeshes);

        // Assert everything went smooth
        var data = bodyVm.Data;

        // Link Data
        data.ArchiveToOutputMap["Meshes/greenBlade.nif"].Should()
            .Be(new GamePath(LocationId.Game, "Data/greenBlade.nif"));
        data.ArchiveToOutputMap["Textures/greenBlade.dds"].Should()
            .Be(new GamePath(LocationId.Game, "Data/greenBlade.dds"));

        // Source Directory Data
        meshes.Status.Should().Be(ModContentNodeStatus.IncludedExplicit);
        textures.Status.Should().Be(ModContentNodeStatus.IncludedExplicit);

        // Preview Data
        var previewData = GetTreeForLocationId(bodyVm.PreviewViewModel, LocationId.Game).Root;
        previewData.GetNode("Data")!.GetNode("greenBlade.nif").Should().NotBeNull();
        previewData.GetNode("Data")!.GetNode("greenBlade.dds").Should().NotBeNull();

        // Shown in preview
        bodyVm.CurrentPreviewViewModel.Should().Be(bodyVm.PreviewViewModel);
    }

    private BodyViewModel CommonSetup(AbsolutePath gameDir, AbsolutePath savesDir)
    {
        var fs = CreateInMemoryFs(gameDir);
        fs.AddPaths(gameDir, GetGameFolderPaths());
        fs.AddSavePaths(savesDir);
        var register = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, fs.FromUnsanitizedFullPath(gameDir.GetFullPath()) },
            { LocationId.Saves, fs.FromUnsanitizedFullPath(savesDir.GetFullPath()) }
        });

        return new BodyViewModel(ModContentVMTestHelpers.CreateTestTreeMSFE(), register);
    }

    public static ISelectLocationTreeViewModel GetTreeForLocationId(ISelectLocationViewModel vm, LocationId id) =>
        vm.AllFoldersTrees.First(x => x.Root.Path.LocationId == id);

    private static ILocationPreviewTreeViewModel GetTreeForLocationId(IPreviewViewModel vm, LocationId id) =>
        vm.Locations.First(x => x.Root.FullPath.LocationId == id);
}
