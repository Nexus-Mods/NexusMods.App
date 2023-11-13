using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ModContent;

// /// <summary>
// ///     Tests related to the linking of mod content nodes to output directories.
// /// </summary>
// public class NodeLinkingTests
// {
//     [Fact]
//     public void CanLinkFolders()
//     {
//         // Arrange & Act
//         var (node, data, target) = CommonSetup();
//         var armorsDir = node.GetNode("Textures").GetNode("Armors");
//         (armorsDir as ModContentTreeEntryViewModel<int>)?.BeginSelect();
//         armorsDir.Link(data, target, false);
//
//         // Assert
//         data.ArchiveToOutputMap.Count.Should().Be(3);
//         data.OutputToArchiveMap.Count.Should().Be(3);
//         armorsDir.GetNode("greenArmor.dds").Status.Should().Be(OldModContentNodeStatus.IncludedViaParent);
//         armorsDir.GetNode("greenBlade.dds").Status.Should().Be(OldModContentNodeStatus.IncludedViaParent);
//         armorsDir.GetNode("greenHilt.dds").Status.Should().Be(OldModContentNodeStatus.IncludedViaParent);
//     }
//
//     [Fact]
//     public void CanLinkFoldersRecursively()
//     {
//         // Arrange & Act
//         var (node, data, target) = CommonSetup();
//         var texturesDir = node.GetNode("Textures");
//         (texturesDir as ModContentTreeEntryViewModel<int>)?.BeginSelect();
//         texturesDir.Link(data, target, false);
//
//         // Assert
//         data.ArchiveToOutputMap.Count.Should().Be(9);
//         data.OutputToArchiveMap.Count.Should().Be(9);
//         AssertArmorsLinked(data, "Armors");
//
//         var armorsDir = texturesDir.GetNode("Armors");
//         armorsDir.GetNode("greenArmor.dds").Status.Should().Be(OldModContentNodeStatus.IncludedViaParent);
//         armorsDir.GetNode("greenBlade.dds").Status.Should().Be(OldModContentNodeStatus.IncludedViaParent);
//         armorsDir.GetNode("greenHilt.dds").Status.Should().Be(OldModContentNodeStatus.IncludedViaParent);
//     }
//
//     [Fact]
//     public void CanLinkFiles()
//     {
//         // Arrange & Act
//         var (node, data, target) = CommonSetup();
//         var greenArmor = node.GetNode("Textures").GetNode("Armors").GetNode("greenArmor.dds");
//         greenArmor.Link(data, target, false);
//
//         // Assert
//         greenArmor.Status.Should().Be(OldModContentNodeStatus.IncludedExplicit);
//         data.ArchiveToOutputMap.Count.Should().Be(1);
//         data.ArchiveToOutputMap["Textures/Armors/greenArmor.dds"].Should()
//             .Be(new GamePath(LocationId.Game, "greenArmor.dds"));
//     }
//
//     // Verifies files can be re-linked.
//     [Fact]
//     public void CanReLinkFiles()
//     {
//         // Arrange & Act
//         var (node, data, target) = CommonSetup();
//         var greenArmor = node.GetNode("Textures").GetNode("Armors").GetNode("greenArmor.dds");
//         greenArmor.Link(data, target, false);
//
//         var greenBlade = node.GetNode("Textures").GetNode("Armors2").GetNode("greenArmor.dds");
//         greenBlade.Link(data, target, false);
//
//         // Assert
//         greenArmor.Status.Should().Be(OldModContentNodeStatus.IncludedExplicit);
//         data.ArchiveToOutputMap.Count.Should().Be(1);
//         data.ArchiveToOutputMap["Textures/Armors2/greenArmor.dds"].Should()
//             .Be(new GamePath(LocationId.Game, "greenArmor.dds"));
//     }
//
//     [Fact]
//     public void CanUnlinkFolders()
//     {
//         // Arrange & Act
//         var (node, data, target) = CommonSetup();
//         var armorsDir = node.GetNode("Textures").GetNode("Armors");
//         armorsDir.Link(data, target, false);
//
//         // Unlink assert that everything is empty.
//         armorsDir.Unlink(false);
//         AssertUnlinkedArmorsFolder(armorsDir, data);
//     }
//
//     [Fact]
//     public void CanUnlinkFolders_ViaUnlinkableItem()
//     {
//         // Arrange & Act
//         var (node, data, target) = CommonSetup();
//         var armorsDir = node.GetNode("Textures").GetNode("Armors");
//         armorsDir.Link(data, target, false);
//
//         // Unlink assert that everything is empty.
//         armorsDir.Unlink(false); // unlinkable
//         AssertUnlinkedArmorsFolder(armorsDir, data);
//     }
//
//     [Fact]
//     public void CanUnlinkFiles()
//     {
//         // Arrange & Act
//         var (node, data, target) = CommonSetup();
//         var greenArmor = node.GetNode("Textures").GetNode("Armors").GetNode("greenArmor.dds");
//         (greenArmor as ModContentTreeEntryViewModel<int>)?.BeginSelect();
//         greenArmor.Link(data, target, false);
//
//         // Assert
//         greenArmor.Unlink(false);
//         data.ArchiveToOutputMap.Count.Should().Be(0);
//         greenArmor.Status.Should().Be(OldModContentNodeStatus.Default);
//     }
//
//     private (ModContentTreeEntryViewModel<int> node, DeploymentData data, IModContentBindingTarget target)
//         CommonSetup()
//     {
//         var node = ModContentVMTestHelpers.CreateTestTreeNode();
//         var data = node.Coordinator.Data;
//         var target = new TestBindingTarget();
//         return (node, data, target);
//     }
//
//     private void AssertArmorsLinked(DeploymentData data, string baseDir = "")
//     {
//         data.ArchiveToOutputMap["Textures/Armors/greenArmor.dds"].Should()
//             .Be(new GamePath(LocationId.Game, $"{baseDir}/greenArmor.dds"));
//         data.ArchiveToOutputMap["Textures/Armors/greenBlade.dds"].Should()
//             .Be(new GamePath(LocationId.Game, $"{baseDir}/greenBlade.dds"));
//         data.ArchiveToOutputMap["Textures/Armors/greenHilt.dds"].Should()
//             .Be(new GamePath(LocationId.Game, $"{baseDir}/greenHilt.dds"));
//     }
//
//     private static void AssertUnlinkedArmorsFolder(IModContentTreeEntryViewModel armorsDir, DeploymentData data)
//     {
//         data.ArchiveToOutputMap.Should().BeEmpty();
//         data.OutputToArchiveMap.Should().BeEmpty();
//         armorsDir.Status.Should().Be(OldModContentNodeStatus.Default);
//         armorsDir.GetNode("greenArmor.dds").Status.Should().Be(OldModContentNodeStatus.Default);
//         armorsDir.GetNode("greenBlade.dds").Status.Should().Be(OldModContentNodeStatus.Default);
//         armorsDir.GetNode("greenHilt.dds").Status.Should().Be(OldModContentNodeStatus.Default);
//     }
//
//     // Stub providing minimum functionality for the tests.
//     public class TestBindingTarget : IModContentBindingTarget
//     {
//         public GamePath Current = new(LocationId.Game, "");
//
//         public IModContentBindingTarget GetOrCreateChild(string name, bool isDirectory)
//         {
//             return new TestBindingTarget()
//             {
//                 Current = new GamePath(LocationId.Game, Current.Path.Join(name))
//             };
//         }
//
//         public GamePath Bind(IUnlinkableItem unlinkable, DeploymentData data, bool previouslyExisted) => Current;
//         public string DirectoryName => Current.FileName;
//         public string FileName { get; } = "";
//
//         public void Unlink(bool isCalledFromDoubleLinkedItem) { }
//     }
// }
