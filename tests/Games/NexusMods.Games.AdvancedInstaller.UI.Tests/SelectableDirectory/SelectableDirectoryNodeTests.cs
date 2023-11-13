using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.SelectableDirectory;

// public class SelectableDirectoryNodeTests
// {
//     [Theory, AutoFileSystem]
//     public void CreateTree(AbsolutePath entryDir)
//     {
//         // Arrange
//         const string rootName = "Skyrim Special Edition";
//         var fs = CreateInMemoryFs(entryDir);
//         fs.AddPaths(entryDir, GetGameFolderPaths());
//
//         // Act
//         var node = SelectableTreeEntryViewModel.Create(fs.FromUnsanitizedFullPath(entryDir.GetFullPath()),
//             new GamePath(LocationId.Game, ""), new DummyCoordinator(), rootName);
//
//         // Assert
//         node.DisplayName.Should().Be(rootName);
//         node.Children.Where(x => x.Status == SelectableDirectoryNodeStatus.Create).Should().HaveCount(1);
//
//         var data = AssertChildNode(node, "Data");
//
//         var textures = AssertChildNode(data, "Textures");
//         textures.Path.Should().Be(new GamePath(LocationId.Game, "Data/Textures"));
//         textures.Children.Where(x => x.Status == SelectableDirectoryNodeStatus.Create).Should().HaveCount(1);
//     }
// }
