using DynamicData;
using FluentAssertions;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.App.UI.Controls;
using NexusMods.Paths;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

/// <summary>
/// Tests for TreeFolderGeneratorCompositeItemModelAdapter: verifies handling of Add, Remove, Update, and Refresh change reasons
/// and ensures correct management of location trees without duplication.
/// </summary>
public class TreeFolderGeneratorCompositeItemModelAdapterTests
{
    /// <summary>
    /// Source of the observable changeset being used during testing as input.
    /// </summary>
    private readonly SourceCache<CompositeItemModel<GamePath>, GamePath> _sourceCache;
    private readonly StubTreeItemFactory _factory = new();
    
    /// <summary>
    /// The adapter under test.
    /// </summary>
    private readonly TreeFolderGeneratorCompositeItemModelAdapter<StubTreeItem, StubTreeItemFactory, GamePath, DefaultFolderModelInitializer<StubTreeItem>> _adapter;
    
    public TreeFolderGeneratorCompositeItemModelAdapterTests() {
        _sourceCache = new SourceCache<CompositeItemModel<GamePath>, GamePath>(model => model.Key);
        _adapter = new TreeFolderGeneratorCompositeItemModelAdapter<StubTreeItem, StubTreeItemFactory, GamePath, DefaultFolderModelInitializer<StubTreeItem>>
            (_factory, _sourceCache.Connect());
    }

    private CompositeItemModel<GamePath> CreateModel(GamePath key) => new(key);

    /// <summary>
    /// Tests that when a model is added to the source cache, the specific file is created
    /// in the location tree with the correct path.
    /// </summary>
    [Fact]
    public void Adapt_Add_CreateSpecificFileInLocationTree()
    {
        // Arrange
        var locationId = LocationId.Game;
        var path = new GamePath(locationId, (RelativePath)"file.txt");
        var model = CreateModel(path);

        // Act
        _sourceCache.AddOrUpdate(model);

        // Assert
        _adapter.FolderGenerator.LocationIdToTree.Should().ContainKey(locationId);
        
        // Access the root folder of the location tree
        var locationTree = _adapter.FolderGenerator.LocationIdToTree[locationId];
        var rootFolder = locationTree.GetOrCreateFolder(GamePath.Empty(locationId), out _);
        
        // Verify the file exists in the root folder
        rootFolder.Files.Count.Should().Be(1);
        rootFolder.Files.Items.First().Key.Should().Be(path);
    }

    /// <summary>
    /// Tests that when a model is added with a nested path, the folder structure is created correctly.
    /// </summary>
    [Fact]
    public void Adapt_Add_CreatesCorrectFolderStructure()
    {
        // Arrange
        var locationId = LocationId.Game;
        var path = new GamePath(locationId, (RelativePath)"folder1/folder2/file.txt");
        var model = CreateModel(path);

        // Act
        _sourceCache.AddOrUpdate(model);

        // Assert
        _adapter.FolderGenerator.LocationIdToTree.Should().ContainKey(locationId);
        
        // Access the location tree
        var locationTree = _adapter.FolderGenerator.LocationIdToTree[locationId];

        // Get the root folder
        var rootFolder = locationTree.GetOrCreateFolder(GamePath.Empty(locationId), out _);
        rootFolder.Folders.Count.Should().Be(1);
        
        // Validate folder1 exists
        var folder1 = rootFolder.Folders.Items.First();
        folder1.FolderName.Should().Be((RelativePath)"folder1");
        folder1.Folders.Count.Should().Be(1);
        
        // Validate folder2 exists
        var folder2 = folder1.Folders.Items.First();
        folder2.FolderName.Should().Be((RelativePath)"folder2");
        folder2.Files.Count.Should().Be(1);
        
        // Validate file exists in folder2
        folder2.Files.Items.First().Key.Should().Be(path);
    }

    /// <summary>
    /// Tests that when the last model associated with a particular location is removed 
    /// from the source cache, the adapter properly removes the entire location tree.
    /// </summary>
    [Fact]
    public void Adapt_Remove_RemovesLocationTree_WhenLastFile()
    {
        // Arrange
        var locationId = LocationId.Saves;
        var path = new GamePath(locationId, (RelativePath)"a.txt");
        var model = CreateModel(path);

        // Act - add
        _sourceCache.AddOrUpdate(model);
        // Assert - added
        _adapter.FolderGenerator.LocationIdToTree.Should().ContainKey(locationId);

        // Act - remove
        _sourceCache.Remove(model);

        // Assert - removed
        _adapter.FolderGenerator.LocationIdToTree.Should().NotContainKey(locationId);
    }

    /// <summary>
    /// Tests that when multiple models are refreshed, no duplicate files are created.
    /// </summary>
    [Fact]
    public void Adapt_Refresh_DoesNotDuplicateFiles()
    {
        // Arrange
        var location = LocationId.Game;

        var path1 = new GamePath(location, (RelativePath)"folder/file1.txt");
        var path2 = new GamePath(location, (RelativePath)"folder/file2.txt");

        var model1 = CreateModel(path1);
        var model2 = CreateModel(path2);

        // Act - add both files
        _sourceCache.AddOrUpdate(model1);
        _sourceCache.AddOrUpdate(model2);
        
        // Get the folder containing the files
        var locationTree = _adapter.FolderGenerator.LocationIdToTree[location];
        var folderPath = new GamePath(location, (RelativePath)"folder");
        var folder = locationTree.GetOrCreateFolder(folderPath, out _);
        
        // Verify initial state
        folder.Files.Count.Should().Be(2);
        
        // Act - refresh all files
        _sourceCache.Refresh(model1);
        _sourceCache.Refresh(model2);

        // Assert - still only 2 files
        folder.Files.Count.Should().Be(2);
        
        // Verify each specific file exists once
        folder.Files.Items.Count(item => item.Key == path1).Should().Be(1);
        folder.Files.Items.Count(item => item.Key == path2).Should().Be(1);
    }
        
    private class StubTreeItem(GamePath path) : ITreeItemWithPath
    {
        public GamePath GetPath() => path;
    }

    private class StubTreeItemFactory : ITreeItemWithPathFactory<GamePath, StubTreeItem>
    {
        public StubTreeItem CreateItem(GamePath key) => new(key);
    }
}
