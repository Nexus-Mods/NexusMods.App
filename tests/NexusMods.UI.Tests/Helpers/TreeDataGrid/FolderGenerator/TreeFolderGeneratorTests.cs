using FluentAssertions;

using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

public class TreeFolderGeneratorTests
{
    [Fact]
    public void OnReceiveFile_AddsRootNode_ForFirstFileInLocation()
    {
        // Arrange
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, (RelativePath)"file.txt");
        var fileModel = CreateFileModel(filePath);
        var testItem = CreateTestTreeItem(filePath);

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
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, (RelativePath)"file.txt");
        var fileModel = CreateFileModel(filePath);
        var testItem = CreateTestTreeItem(filePath);

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
        var locationId1 = LocationId.Game;
        var filePath1 = new GamePath(locationId1, (RelativePath)"file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);

        var locationId2 = LocationId.Saves;
        var filePath2 = new GamePath(locationId2, (RelativePath)"file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);

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
        
        var filePath1 = new GamePath(locationId, (RelativePath)"file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        
        var filePath2 = new GamePath(locationId, (RelativePath)"file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);

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
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, (RelativePath)"file.txt");
        var fileModel = CreateFileModel(filePath);
        var testItem = CreateTestTreeItem(filePath);

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
    public void ObservableRoots_EmitsChange_WhenFirstItemAdded()
    {
        // Arrange
        var generator = CreateGenerator();
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, (RelativePath)"file.txt");
        var fileModel = CreateFileModel(filePath);
        var testItem = CreateTestTreeItem(filePath);
        
        var addedItems = new List<CompositeItemModel<GamePath>>();
        
        // Subscribe to the observable
        using var subscription = generator.ObservableRoots()
            .Subscribe(changes => 
            {
                foreach (var change in changes)
                {
                    if (change.Reason == DynamicData.ChangeReason.Add)
                    {
                        addedItems.Add(change.Current);
                    }
                }
            });
            
        // Act
        generator.OnReceiveFile(testItem, fileModel);
        
        // Assert
        addedItems.Should().HaveCount(1, "one root item should be added");
    }
    
    [Fact]
    public void ObservableRoots_DoesNotEmitChange_WhenItemAddedToExistingRoot()
    {
        // Arrange
        var generator = CreateGenerator();
        var locationId = LocationId.Game;
        
        // Add first file to create the root
        var filePath1 = new GamePath(locationId, (RelativePath)"file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        generator.OnReceiveFile(testItem1, fileModel1);
        
        // Prepare second file
        var filePath2 = new GamePath(locationId, (RelativePath)"file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);
        
        var rootChangesDetected = false;
        
        // Subscribe to the observable after first file added
        using var subscription = generator.ObservableRoots()
            .Subscribe(_ => 
            {
                rootChangesDetected = true;
            });
            
        // Act - add second file to existing root
        rootChangesDetected = false;
        generator.OnReceiveFile(testItem2, fileModel2);
        
        // Assert
        rootChangesDetected.Should().BeFalse("no root-level changes should occur when adding to existing root");
    }
    
    [Fact]
    public void ObservableRoots_EmitsChange_WhenNewRootAdded()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Add first file to create the first root
        var locationId1 = LocationId.Game;
        var filePath1 = new GamePath(locationId1, (RelativePath)"file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        generator.OnReceiveFile(testItem1, fileModel1);
        
        // Prepare file for second root
        var locationId2 = LocationId.Saves;
        var filePath2 = new GamePath(locationId2, (RelativePath)"file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);
        
        var addedItems = new List<CompositeItemModel<GamePath>>();
        
        // Subscribe to the observable after first file added
        using var subscription = generator.ObservableRoots()
            .Subscribe(changes => 
            {
                foreach (var change in changes)
                {
                    if (change.Reason == DynamicData.ChangeReason.Add)
                    {
                        addedItems.Add(change.Current);
                    }
                }
            });
            
        // Act - add file to new root
        generator.OnReceiveFile(testItem2, fileModel2);
        
        // Assert
        addedItems.Should().HaveCount(2, "two root items should be added");
    }
    
    [Fact]
    public void ObservableRoots_EmitsChange_WhenRootRemoved()
    {
        // Arrange
        var generator = CreateGenerator();
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, (RelativePath)"file.txt");
        var fileModel = CreateFileModel(filePath);
        var testItem = CreateTestTreeItem(filePath);
        
        // Add a file to create the root
        generator.OnReceiveFile(testItem, fileModel);
        var removedItems = new List<CompositeItemModel<GamePath>>();
        
        // Subscribe to the observable after file is added
        using var subscription = generator.ObservableRoots()
            .Subscribe(changes => 
            {
                foreach (var change in changes)
                {
                    if (change.Reason == DynamicData.ChangeReason.Remove)
                    {
                        removedItems.Add(change.Current);
                    }
                }
            });
            
        // Act - remove the file, which should remove the root
        generator.OnDeleteFile(testItem, fileModel);
        
        // Assert
        removedItems.Should().HaveCount(1, "one root item should be removed");
    }
    
    [Fact]
    public void ObservableRoots_DoesNotEmitChange_WhenNonLastItemRemoved()
    {
        // Arrange
        var generator = CreateGenerator();
        var locationId = LocationId.Game;
        
        // Add two files to the same root
        var filePath1 = new GamePath(locationId, (RelativePath)"file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        
        var filePath2 = new GamePath(locationId, (RelativePath)"file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);
        
        generator.OnReceiveFile(testItem1, fileModel1);
        generator.OnReceiveFile(testItem2, fileModel2);
        
        var rootChangesDetected = false;
        
        // Subscribe to the observable after both files are added
        using var subscription = generator.ObservableRoots()
            .Subscribe(_ => 
            {
                rootChangesDetected = true;
            });
            
        // Act - remove one file but not the last one
        rootChangesDetected = false;
        generator.OnDeleteFile(testItem1, fileModel1);
        
        // Assert
        rootChangesDetected.Should().BeFalse("no root-level changes should occur when removing a non-last item");
    }

    [Fact]
    public void OnDeleteFile_DoesNothing_WhenFileNotFound()
    {
        // Arrange
        var locationId = LocationId.Game;
        var filePath = new GamePath(locationId, (RelativePath)"file.txt");
        var fileModel = CreateFileModel(filePath);
        var testItem = CreateTestTreeItem(filePath);

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
        
        var filePath1 = new GamePath(locationId, (RelativePath)"folder1/file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        
        var filePath2 = new GamePath(locationId, (RelativePath)"folder1/folder2/file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);

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
        
        var filePath1 = new GamePath(locationId1, (RelativePath)"folder1/file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        
        var filePath2 = new GamePath(locationId1, (RelativePath)"folder1/folder2/file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);

        var filePath3 = new GamePath(locationId2, (RelativePath)"file3.txt");
        var fileModel3 = CreateFileModel(filePath3);
        var testItem3 = CreateTestTreeItem(filePath3);

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

    [Fact]
    public void SimplifiedObservableRoots_ReturnsNoItems_WhenNoRootsExist()
    {
        // Arrange
        var generator = CreateGenerator();
        var itemsReceived = new List<CompositeItemModel<GamePath>>();
        
        // Act - subscribe to the simplified observable
        using var subscription = generator.SimplifiedObservableRoots()
            .Subscribe(changes => 
            {
                foreach (var change in changes)
                    if (change.Reason == DynamicData.ChangeReason.Add)
                        itemsReceived.Add(change.Current);
            });
            
        // Assert
        itemsReceived.Should().BeEmpty("no items should be emitted when no roots exist");
    }
    
    [Fact]
    public void SimplifiedObservableRoots_ReturnsChildrenOfRoot_WhenExactlyOneRootExists()
    {
        // Note(sewer): In this test we create 2 items which are both in `LocationId.Game`.
        //              What this means is that the `TreeFolderGenerator` will only have 1
        //              root; that is `Game`.
        //
        //              Because there is only 1 root, the 'simplified'
        //              observable will skip the layer with the `Game` folder, and return the
        //              children of the `Game` folder directly instead.
        
        // Arrange
        var generator = CreateGenerator();
        var locationId = LocationId.Game;
        
        // Create files with a folder structure
        var filePath1 = new GamePath(locationId, (RelativePath)"folder1/file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        
        var filePath2 = new GamePath(locationId, (RelativePath)"folder2/file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);
        
        // Add files to create the root and folder structure
        generator.OnReceiveFile(testItem1, fileModel1);
        generator.OnReceiveFile(testItem2, fileModel2);
        
        var itemsReceived = new List<CompositeItemModel<GamePath>>();
        
        // Act - subscribe to the simplified observable
        using var subscription = generator.SimplifiedObservableRoots()
            .Subscribe(changes => 
            {
                foreach (var change in changes)
                    if (change.Reason == DynamicData.ChangeReason.Add)
                        itemsReceived.Add(change.Current);
            });
            
        // Assert - should return the top-level folders, not the location root
        itemsReceived.Should().NotBeEmpty("children of the single root should be emitted");
        itemsReceived.Count.Should().Be(2, "two top-level folders should be emitted");
        
        // Verify we're getting folders, not the location root
        foreach (var item in itemsReceived)
            item.Key.Should().NotBe(GamePath.Empty(locationId), "the location ID itself should not be emitted");
    }
    
    [Fact]
    public void SimplifiedObservableRoots_ReturnsAllRoots_WhenMultipleRootsExist()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Create files in two different locations
        var locationId1 = LocationId.Game;
        var filePath1 = new GamePath(locationId1, (RelativePath)"file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        
        var locationId2 = LocationId.Saves;
        var filePath2 = new GamePath(locationId2, (RelativePath)"file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);
        
        // Add files to create the roots
        generator.OnReceiveFile(testItem1, fileModel1);
        generator.OnReceiveFile(testItem2, fileModel2);
        
        var rootsReceived = new List<CompositeItemModel<GamePath>>();
        
        // Act - subscribe to the simplified observable
        using var subscription = generator.SimplifiedObservableRoots()
            .Subscribe(changes => 
            {
                foreach (var change in changes)
                    if (change.Reason == DynamicData.ChangeReason.Add)
                        rootsReceived.Add(change.Current);
            });
            
        // Assert - should return both location roots
        rootsReceived.Should().HaveCount(2, "both location roots should be emitted");
    }
    
    [Fact]
    public void SimplifiedObservableRoots_SwitchesBehavior_WhenRootCountChanges()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // First file in first location
        var locationId1 = LocationId.Game;
        var filePath1 = new GamePath(locationId1, (RelativePath)"folder1/file1.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var testItem1 = CreateTestTreeItem(filePath1);
        
        // Second file in second location 
        var locationId2 = LocationId.Saves;
        var filePath2 = new GamePath(locationId2, (RelativePath)"file2.txt");
        var fileModel2 = CreateFileModel(filePath2);
        var testItem2 = CreateTestTreeItem(filePath2);
        
        // Add first file to create one root
        generator.OnReceiveFile(testItem1, fileModel1);
        
        var allItemsReceived = new List<CompositeItemModel<GamePath>>();
        var removedItems = new List<CompositeItemModel<GamePath>>();
        
        // Subscribe to the simplified observable
        using var subscription = generator.SimplifiedObservableRoots()
            .Subscribe(changes => 
            {
                foreach (var change in changes)
                {
                    if (change.Reason == DynamicData.ChangeReason.Add)
                    {
                        allItemsReceived.Add(change.Current);
                    }
                    else if (change.Reason == DynamicData.ChangeReason.Remove)
                    {
                        removedItems.Add(change.Current);
                    }
                }
            });
        
        // Act 1 - clear existing items and record current count
        allItemsReceived.Clear();
        removedItems.Clear();
        var itemsWithOneRoot = allItemsReceived.Count;
        
        // Act 2 - add second file to create second root
        generator.OnReceiveFile(testItem2, fileModel2);
        
        // Assert - when going from one root to multiple roots
        removedItems.Should().NotBeEmpty("existing children should be removed when switching to multiple roots");
        allItemsReceived.Should().HaveCount(2, "both location roots should be emitted");
        
        // Act 3 - clear items again and remove second root
        allItemsReceived.Clear();
        removedItems.Clear();
        
        // Remove the file in the second location
        generator.OnDeleteFile(testItem2, fileModel2);
        
        // Assert - when going back to one root from multiple roots
        removedItems.Should().NotBeEmpty("roots should be removed when switching back to single root");
        allItemsReceived.Should().NotBeEmpty("children of single root should be emitted");
    }

    private static TreeFolderGenerator<TestTreeItemWithPath, DefaultFolderModelInitializer<TestTreeItemWithPath>> CreateGenerator() => new();

    private static CompositeItemModel<GamePath> CreateFileModel(GamePath path)
    {
        return new CompositeItemModel<GamePath>(path);
    }

    private static TestTreeItemWithPath CreateTestTreeItem(GamePath path)
    {
        return new TestTreeItemWithPath
        {
            Key = path,
            LocationId = path.LocationId,
            RelativePath = path.Path,
            Name = path.Path.FileName
        };
    }
}
