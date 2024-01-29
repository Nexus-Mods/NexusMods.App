using FluentAssertions;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests;

// ReSharper disable once InconsistentNaming
public class AdvancedInstallerUITests
{
    private BodyViewModel CommonBodyVMSetup(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        var deploymentData = new DeploymentData();
        var fileTreeNode = AdvancedInstallerTestHelpers.CreateTestFileTree();
        var fs = AdvancedInstallerTestHelpers.CreateInMemoryFs();

        var gameLocationsRegister = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, fs.FromUnsanitizedFullPath(gameDir.GetFullPath()) },
            {
                LocationId.From("Data"),
                fs.FromUnsanitizedFullPath(gameDir.Combine(new RelativePath("Data")).GetFullPath())
            },
            { LocationId.AppData, fs.FromUnsanitizedFullPath(appdataDir.GetFullPath()) }
        });

        Loadout? loadout = null;
        return new BodyViewModel(deploymentData, "some-mod-name", fileTreeNode, gameLocationsRegister, loadout,
            "some-game-name");
    }

    [Theory, AutoFileSystem]
    public void SelectingModEntrySelectsChildren(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        // Arrange
        var bodyVm = CommonBodyVMSetup(gameDir, appdataDir);
        var modContentVm = bodyVm.ModContentViewModel;
        var entryId = new RelativePath("Blue Version/Data");

        // Act
        var modEntry = modContentVm.ModContentEntriesCache.Lookup(entryId);
        modEntry.HasValue.Should().BeTrue();
        bodyVm.OnBeginSelect(modEntry.Value);

        // Assert
        modEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Selecting);
        var node = modContentVm.Root.GetTreeNode(entryId);
        node.HasValue.Should().BeTrue();
        var descendentNodes = node.Value.GetAllDescendentNodes();
        descendentNodes.Should().HaveCount(7);
        descendentNodes.Should().OnlyContain(x => x.Item.Status == ModContentTreeEntryStatus.SelectingViaParent);
    }


    [Theory, AutoFileSystem]
    public void DeselectingModEntryDeselectsChildren(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        // Arrange
        var bodyVm = CommonBodyVMSetup(gameDir, appdataDir);
        var modContentVm = bodyVm.ModContentViewModel;
        var entryId = new RelativePath("Blue Version/Data");

        // Act
        var modEntry = modContentVm.ModContentEntriesCache.Lookup(entryId);
        modEntry.HasValue.Should().BeTrue();
        bodyVm.OnBeginSelect(modEntry.Value);
        bodyVm.OnCancelSelect(modEntry.Value);

        // Assert
        modEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Default);
        var node = modContentVm.Root.GetTreeNode(entryId);
        node.HasValue.Should().BeTrue();
        var descendentNodes = node.Value.GetAllDescendentNodes();
        descendentNodes.Should().HaveCount(7);
        descendentNodes.Should().OnlyContain(x => x.Item.Status == ModContentTreeEntryStatus.Default);
    }

    [Theory, AutoFileSystem]
    public void DeselectingChildrenDeselectsParent(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        // Arrange
        var bodyVm = CommonBodyVMSetup(gameDir, appdataDir);
        var modContentVm = bodyVm.ModContentViewModel;
        var entryId = new RelativePath("Blue Version/Data/Textures");

        // Select Textures folder
        var modEntry = modContentVm.ModContentEntriesCache.Lookup(entryId);
        modEntry.HasValue.Should().BeTrue();
        bodyVm.OnBeginSelect(modEntry.Value);

        modEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Selecting);
        var node = modContentVm.Root.GetTreeNode(entryId);
        node.HasValue.Should().BeTrue();
        var descendentNodes = node.Value.GetAllDescendentNodes();
        descendentNodes.Should().HaveCount(2);
        descendentNodes.Should().OnlyContain(x => x.Item.Status == ModContentTreeEntryStatus.SelectingViaParent);

        // Deselect the children
        descendentNodes.ForEach(x => bodyVm.OnCancelSelect(x.Item));
        // Parent should get deselected as well
        modEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Default);
    }

    [Theory, AutoFileSystem]
    public void MultipleSelectionsDontInteract(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        // Arrange
        var bodyVm = CommonBodyVMSetup(gameDir, appdataDir);
        var modContentVm = bodyVm.ModContentViewModel;
        var entryId = new RelativePath("Blue Version/Data");
        var separateEntryId = new RelativePath("Blue Version/Data/Textures");

        // Select Textures folder
        var separateSelectionEntry = modContentVm.ModContentEntriesCache.Lookup(separateEntryId);
        separateSelectionEntry.HasValue.Should().BeTrue();
        bodyVm.OnBeginSelect(separateSelectionEntry.Value);

        // Select Data folder
        var modEntry = modContentVm.ModContentEntriesCache.Lookup(entryId);
        modEntry.HasValue.Should().BeTrue();
        bodyVm.OnBeginSelect(modEntry.Value);

        modEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Selecting);
        separateSelectionEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Selecting);

        bodyVm.OnCancelSelect(modEntry.Value);
        modEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Default);

        // Textures should still be selected
        separateSelectionEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Selecting);
        // Texture children should still be selected
        modContentVm.ModContentEntriesCache.Lookup(new RelativePath("Blue Version/Data/Textures/textureA.dds"))
            .Value.Status.Should().Be(ModContentTreeEntryStatus.SelectingViaParent);
    }

    [Theory, AutoFileSystem]
    public void SelectableTreeNodesHaveCreateFolderChildren(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        // Arrange
        var bodyVm = CommonBodyVMSetup(gameDir, appdataDir);

        // Check all the locations are present in SuggestedEntries
        bodyVm.SelectLocationViewModel.SuggestedEntries.Should().HaveCount(3);
        bodyVm.SelectLocationViewModel.SuggestedEntries.Should().Contain(entry =>
            entry.RelativeToTopLevelLocation == new GamePath(LocationId.Game, "Data"));

        // Check if nested location is present in the tree
        bodyVm.SelectLocationViewModel.TreeRoots
            .GetTreeNode(new GamePath(LocationId.Game, "")).Value.Children
            .Should()
            .Contain(entry => entry.Id == new GamePath(LocationId.Game, "Data"));

        // Number of CreateFolder should match number of regula folders
        bodyVm.SelectLocationViewModel.TreeEntriesCache.Items
            .Where(entry => entry.Status == SelectableDirectoryNodeStatus.Regular)
            .Should()
            .HaveCount(bodyVm.SelectLocationViewModel.TreeEntriesCache.Items
                .Count(entry => entry.Status == SelectableDirectoryNodeStatus.Create));
    }

    [Theory, AutoFileSystem]
    public void ComplexMappingSequenceShouldOutputExpected(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        // Arrange
        var bodyVm = CommonBodyVMSetup(gameDir, appdataDir);

        var blueDataModEntry =
            bodyVm.ModContentViewModel.ModContentEntriesCache.Lookup(new RelativePath("Blue Version/Data"));
        var blueTextureEntry =
            bodyVm.ModContentViewModel.ModContentEntriesCache.Lookup(
                new RelativePath("Blue Version/Data/Textures/textureB.dds"));
        var greenDataModEntry =
            bodyVm.ModContentViewModel.ModContentEntriesCache.Lookup(new RelativePath("Green Version/Data"));

        var selectableGameEntry =
            bodyVm.SelectLocationViewModel.TreeEntriesCache.Lookup(new GamePath(LocationId.Game, ""));

        // Select Blue Data
        bodyVm.OnBeginSelect(blueDataModEntry.Value);
        // Remove a texture from the selection
        bodyVm.OnCancelSelect(blueTextureEntry.Value);

        // Map Blue Data to Game
        bodyVm.OnCreateMapping(selectableGameEntry.Value);

        // Check deployment data
        bodyVm.DeploymentData.ArchiveToOutputMap.Should().HaveCount(5);
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should()
            .BeEquivalentTo(
                new Dictionary<RelativePath, GamePath>()
                {
                    {
                        new RelativePath("Blue Version/Data/file1.txt"), new GamePath(LocationId.Game, "Data/file1.txt")
                    },
                    {
                        new RelativePath("Blue Version/Data/file2.txt"), new GamePath(LocationId.Game, "Data/file2.txt")
                    },
                    {
                        new RelativePath("Blue Version/Data/pluginA.esp"),
                        new GamePath(LocationId.Game, "Data/pluginA.esp")
                    },
                    {
                        new RelativePath("Blue Version/Data/PluginB.esp"),
                        new GamePath(LocationId.Game, "Data/PluginB.esp")
                    },
                    {
                        new RelativePath("Blue Version/Data/Textures/textureA.dds"),
                        new GamePath(LocationId.Game, "Data/Textures/textureA.dds")
                    },
                });

        // Add Green Data
        bodyVm.OnBeginSelect(greenDataModEntry.Value);
        bodyVm.OnCreateMapping(selectableGameEntry.Value);

        // Check PreviewTree
        bodyVm.PreviewViewModel.TreeRoots.Should().ContainSingle();
        // 8 files + Data + Textures = 10
        bodyVm.PreviewViewModel.TreeRoots
            .GetTreeNode(new GamePath(LocationId.Game, "")).Value.GetAllDescendentNodes().Should().HaveCount(10);

        // Check deployment data
        bodyVm.DeploymentData.ArchiveToOutputMap.Should().HaveCount(8);
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should()
            .BeEquivalentTo(
                new Dictionary<RelativePath, GamePath>()
                {
                    {
                        new RelativePath("Green Version/Data/file1.txt"),
                        new GamePath(LocationId.Game, "Data/file1.txt")
                    },
                    {
                        new RelativePath("Blue Version/Data/file2.txt"), new GamePath(LocationId.Game, "Data/file2.txt")
                    },
                    {
                        new RelativePath("Green Version/Data/file3.txt"),
                        new GamePath(LocationId.Game, "Data/file3.txt")
                    },
                    {
                        new RelativePath("Green Version/Data/pluginA.esp"),
                        new GamePath(LocationId.Game, "Data/pluginA.esp")
                    },
                    {
                        new RelativePath("Blue Version/Data/PluginB.esp"),
                        new GamePath(LocationId.Game, "Data/PluginB.esp")
                    },
                    {
                        new RelativePath("Green Version/Data/PluginC.esp"),
                        new GamePath(LocationId.Game, "Data/PluginC.esp")
                    },
                    {
                        new RelativePath("Green Version/Data/Textures/textureA.dds"),
                        new GamePath(LocationId.Game, "Data/Textures/textureA.dds")
                    },
                    {
                        new RelativePath("Green Version/Data/Textures/textureC.dds"),
                        new GamePath(LocationId.Game, "Data/Textures/textureC.dds")
                    },
                });

        // Check output
        bodyVm.DeploymentData.EmitOperations(AdvancedInstallerTestHelpers.CreateTestFileTree())
            .Select(aModFile =>
            {
                aModFile.Should().BeOfType<StoredFile>();
                return (aModFile as StoredFile)!.To;
            }).Should().BeEquivalentTo(
                new List<GamePath>()
                {
                    new(LocationId.Game, "Data/file1.txt"),
                    new(LocationId.Game, "Data/file2.txt"),
                    new(LocationId.Game, "Data/file3.txt"),
                    new(LocationId.Game, "Data/pluginA.esp"),
                    new(LocationId.Game, "Data/PluginB.esp"),
                    new(LocationId.Game, "Data/PluginC.esp"),
                    new(LocationId.Game, "Data/Textures/textureA.dds"),
                    new(LocationId.Game, "Data/Textures/textureC.dds"),
                });

        // Remove Green Data from mod contents
        bodyVm.OnRemoveMappingFromModContent(greenDataModEntry.Value);

        // Check Preview tree
        bodyVm.PreviewViewModel.TreeEntriesCache.Items.Should().HaveCount(4);
        bodyVm.PreviewViewModel.TreeEntriesCache.Items
            .Select(entry => entry.GamePath)
            .Should()
            .BeEquivalentTo(
                new List<GamePath>()
                {
                    new(LocationId.Game, ""),
                    new(LocationId.Game, "Data"),
                    new(LocationId.Game, "Data/file2.txt"),
                    new(LocationId.Game, "Data/PluginB.esp"),
                });

        // Check mod contents tree
        bodyVm.ModContentViewModel.ModContentEntriesCache.Items
            .Count(entry => entry.Status == ModContentTreeEntryStatus.IncludedExplicit).Should().Be(1);
        blueDataModEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.IncludedExplicit);
        blueTextureEntry.Value.Status.Should().Be(ModContentTreeEntryStatus.Default);
        bodyVm.ModContentViewModel.ModContentEntriesCache.Items
            .Count(entry => entry.Status == ModContentTreeEntryStatus.IncludedViaParent).Should().Be(2);


        // Check deployment data
        bodyVm.DeploymentData.ArchiveToOutputMap.Should().HaveCount(2);
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should()
            .BeEquivalentTo(
                new Dictionary<RelativePath, GamePath>()
                {
                    {
                        new RelativePath("Blue Version/Data/file2.txt"), new GamePath(LocationId.Game, "Data/file2.txt")
                    },
                    {
                        new RelativePath("Blue Version/Data/PluginB.esp"),
                        new GamePath(LocationId.Game, "Data/PluginB.esp")
                    },
                });

        // Remove one Preview file
        var previewDataEntry =
            bodyVm.PreviewViewModel.TreeEntriesCache.Lookup(new GamePath(LocationId.Game, "Data/PluginB.esp"));
        bodyVm.OnRemoveEntryFromPreview(previewDataEntry.Value);

        // Check deployment data
        bodyVm.DeploymentData.ArchiveToOutputMap.Should().ContainSingle();
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should()
            .BeEquivalentTo(
                new Dictionary<RelativePath, GamePath>()
                {
                    {
                        new RelativePath("Blue Version/Data/file2.txt"), new GamePath(LocationId.Game, "Data/file2.txt")
                    },
                });

        // Remove the last Preview file
        bodyVm.OnRemoveEntryFromPreview(bodyVm.PreviewViewModel.TreeEntriesCache
            .Lookup(new GamePath(LocationId.Game, "Data/file2.txt")).Value);


        // Check preview is empty
        bodyVm.PreviewViewModel.TreeEntriesCache.Items.Should().BeEmpty();
        bodyVm.PreviewViewModel.TreeRoots.Should().BeEmpty();
        bodyVm.CurrentRightContentViewModel.Should().Be(bodyVm.EmptyPreviewViewModel);

        // Check mod contents tree
        bodyVm.ModContentViewModel.ModContentEntriesCache.Items
            .Count(entry =>
                entry.Status is ModContentTreeEntryStatus.IncludedExplicit
                    or ModContentTreeEntryStatus.IncludedViaParent).Should().Be(0);

        // Check deployment data
        bodyVm.DeploymentData.ArchiveToOutputMap.Should().BeEmpty();
    }

    [Theory, AutoFileSystem]
    public void MappingRootMapsChildrenDirectly(AbsolutePath gameDir, AbsolutePath appdataDir)
    {
        // Arrange
        var bodyVm = CommonBodyVMSetup(gameDir, appdataDir);
        var blueTextureEntry =
            bodyVm.ModContentViewModel.ModContentEntriesCache.Lookup(
                new RelativePath("Blue Version/Data/Textures/textureA.dds"));

        // Select a texture file first
        bodyVm.OnBeginSelect(blueTextureEntry.Value);

        // Select the root
        bodyVm.OnBeginSelect(bodyVm.ModContentViewModel.Root.Item);

        // Map to game
        var selectableGameEntry =
            bodyVm.SelectLocationViewModel.TreeEntriesCache.Lookup(new GamePath(LocationId.Game, ""));
        bodyVm.OnCreateMapping(selectableGameEntry.Value);

        // Check deployment data
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should()
            .Contain(entry => entry.Key == new RelativePath("Blue Version/Data/Textures/textureA.dds") &&
                              entry.Value == new GamePath(LocationId.Game, "textureA.dds"));
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should()
            .Contain(entry => entry.Key == new RelativePath("Blue Version/Data/Textures/textureB.dds") &&
                              entry.Value == new GamePath(LocationId.Game, "Blue Version/Data/Textures/textureB.dds"));
        // Check preview tree
        bodyVm.PreviewViewModel.TreeRoots.GetTreeNode(new GamePath(LocationId.Game, "")).Value.Children
            .Should().HaveCount(3);

        // Remove the root mapping
        bodyVm.OnRemoveMappingFromModContent(bodyVm.ModContentViewModel.Root.Item);

        // Check preview tree
        bodyVm.PreviewViewModel.TreeEntriesCache.Items.Should().HaveCount(2);


        // Check deployment data
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should().ContainSingle();
        bodyVm.DeploymentData.ArchiveToOutputMap
            .Should()
            .Contain(entry => entry.Key == new RelativePath("Blue Version/Data/Textures/textureA.dds") &&
                              entry.Value == new GamePath(LocationId.Game, "textureA.dds"));
    }
}
