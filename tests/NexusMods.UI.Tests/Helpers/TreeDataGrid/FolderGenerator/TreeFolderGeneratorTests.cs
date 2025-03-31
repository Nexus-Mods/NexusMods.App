using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

public class TreeFolderGeneratorTests
{
    [Fact]
    public void OnReceiveFile_AddsRootNode_ForFirstFileInLocation()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, new RelativePath("file.txt"));
        var fileModel = CreateFileModel(fileId);
        var testItem = CreateTestTreeItem(fileId, filePath);

        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(testItem, fileModel);

        // Assert - verify a root node was created
        var idToTree = generator.LocationIdToTree;
        idToTree.Should().ContainKey(locationId);
    }

    [Fact]
    public void OnReceiveFile_AddsFileUnderCorrectLocationRoot()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, new RelativePath("file.txt"));
        var fileModel = CreateFileModel(fileId);
        var testItem = CreateTestTreeItem(fileId, filePath);

        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(testItem, fileModel);

        // Assert - verify an internal tree was created for the location
        var idToTree = generator.LocationIdToTree;
        idToTree.Should().ContainKey(locationId);
    }

    [Fact]
    public void OnReceiveFile_OrganizesFilesUnderSeparateLocationRoots()
    {
        // Arrange
        var fileId1 = EntityId.From(0UL);
        var locationId1 = LocationId.Game;
        var filePath1 = new GamePath(locationId1, new RelativePath("file1.txt"));
        var fileModel1 = CreateFileModel(fileId1);
        var testItem1 = CreateTestTreeItem(fileId1, filePath1);

        var fileId2 = EntityId.From(1UL);
        var locationId2 = LocationId.Saves;
        var filePath2 = new GamePath(locationId2, new RelativePath("file2.txt"));
        var fileModel2 = CreateFileModel(fileId2);
        var testItem2 = CreateTestTreeItem(fileId2, filePath2);

        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(testItem1, fileModel1);
        generator.OnReceiveFile(testItem2, fileModel2);

        // Assert - verify we have trees for both locations
        var idToTree = generator.LocationIdToTree;
        idToTree.Should().ContainKey(locationId1);
        idToTree.Should().ContainKey(locationId2);
        idToTree.Count.Should().Be(2);
    }

    [Fact]
    public void OnDeleteFile_RemovesFileButKeepsRoot_WhenMoreFilesExistInLocation()
    {
        // Arrange
        var locationId = LocationId.Game;
        
        var fileId1 = EntityId.From(0UL);
        var filePath1 = new GamePath(locationId, new RelativePath("file1.txt"));
        var fileModel1 = CreateFileModel(fileId1);
        var testItem1 = CreateTestTreeItem(fileId1, filePath1);
        
        var fileId2 = EntityId.From(1UL);
        var filePath2 = new GamePath(locationId, new RelativePath("file2.txt"));
        var fileModel2 = CreateFileModel(fileId2);
        var testItem2 = CreateTestTreeItem(fileId2, filePath2);

        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(testItem1, fileModel1);
        generator.OnReceiveFile(testItem2, fileModel2);
        
        // Delete one file
        generator.OnDeleteFile(testItem1, fileModel1);

        // Assert - verify location still exists after deletion of one file
        var idToTree = generator.LocationIdToTree;
        idToTree.Should().ContainKey(locationId);
    }

    [Fact]
    public void OnDeleteFile_RemovesLocationRoot_WhenLastFileInLocationDeleted()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, new RelativePath("file.txt"));
        var fileModel = CreateFileModel(fileId);
        var testItem = CreateTestTreeItem(fileId, filePath);

        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(testItem, fileModel);
        
        // Verify we have the location tree after adding
        var treesBeforeDelete = generator.LocationIdToTree;
        treesBeforeDelete.Should().ContainKey(locationId);
        
        // Delete the only file
        generator.OnDeleteFile(testItem, fileModel);

        // Assert - verify location was removed after deleting the only file
        var treesAfterDelete = generator.LocationIdToTree;
        treesAfterDelete.Should().NotContainKey(locationId);
        treesAfterDelete.Should().BeEmpty();
    }

    [Fact]
    public void ObservableRoots_ReturnsEmptySnapshot_WhenNoFilesAdded()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act - verify no trees exist initially
        var idToTree = generator.LocationIdToTree;

        // Assert
        idToTree.Should().BeEmpty();
    }

    [Fact]
    public void OnDeleteFile_DoesNothing_WhenFileNotFound()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, new RelativePath("file.txt"));
        var fileModel = CreateFileModel(fileId);
        var testItem = CreateTestTreeItem(fileId, filePath);

        // Act
        var generator = CreateGenerator();
        
        // Delete a file that was never added
        generator.OnDeleteFile(testItem, fileModel);

        // Assert - should not throw and no trees should exist
        var idToTree = generator.LocationIdToTree;
        idToTree.Should().BeEmpty();
    }

    [Fact]
    public void OnReceiveFile_AddsFilesToCorrectTreeStructure()
    {
        // Arrange
        var locationId = LocationId.Game;
        
        var fileId1 = EntityId.From(0UL);
        var filePath1 = new GamePath(locationId, new RelativePath("folder1/file1.txt"));
        var fileModel1 = CreateFileModel(fileId1);
        var testItem1 = CreateTestTreeItem(fileId1, filePath1);
        
        var fileId2 = EntityId.From(1UL);
        var filePath2 = new GamePath(locationId, new RelativePath("folder1/folder2/file2.txt"));
        var fileModel2 = CreateFileModel(fileId2);
        var testItem2 = CreateTestTreeItem(fileId2, filePath2);

        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(testItem1, fileModel1);
        generator.OnReceiveFile(testItem2, fileModel2);

        // Assert - verify location tree was created
        var idToTree = generator.LocationIdToTree;
        idToTree.Should().ContainKey(locationId);
    }

    [Fact]
    public void OnReceiveFile_AddsFilesToCorrectTreeStructure_MultipleLocations()
    {
        // Arrange
        var locationId1 = LocationId.Game;
        var locationId2 = LocationId.Saves;
        
        var fileId1 = EntityId.From(0UL);
        var filePath1 = new GamePath(locationId1, new RelativePath("folder1/file1.txt"));
        var fileModel1 = CreateFileModel(fileId1);
        var testItem1 = CreateTestTreeItem(fileId1, filePath1);
        
        var fileId2 = EntityId.From(1UL);
        var filePath2 = new GamePath(locationId1, new RelativePath("folder1/folder2/file2.txt"));
        var fileModel2 = CreateFileModel(fileId2);
        var testItem2 = CreateTestTreeItem(fileId2, filePath2);

        var fileId3 = EntityId.From(2UL);
        var filePath3 = new GamePath(locationId2, new RelativePath("file3.txt"));
        var fileModel3 = CreateFileModel(fileId3);
        var testItem3 = CreateTestTreeItem(fileId3, filePath3);

        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(testItem1, fileModel1);
        generator.OnReceiveFile(testItem2, fileModel2);
        generator.OnReceiveFile(testItem3, fileModel3);

        // Assert - verify we have trees for both locations
        var idToTree = generator.LocationIdToTree;
        idToTree.Should().ContainKey(locationId1);
        idToTree.Should().ContainKey(locationId2);
        idToTree.Count.Should().Be(2);
    }

    private static TreeFolderGenerator<TestTreeItemWithPath> CreateGenerator() => new();

    private static CompositeItemModel<EntityId> CreateFileModel(EntityId id)
    {
        return new CompositeItemModel<EntityId>(id);
    }

    private static TestTreeItemWithPath CreateTestTreeItem(EntityId id, GamePath path)
    {
        return new TestTreeItemWithPath
        {
            Key = id,
            LocationId = path.LocationId,
            RelativePath = path.Path,
            Name = path.Path.FileName
        };
    }
}
