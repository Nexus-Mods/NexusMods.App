using DynamicData;
using FluentAssertions;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
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
    private readonly SourceCache<CompositeItemModel<EntityId>, EntityId> _sourceCache;
    private readonly StubTreeItemFactory _factory = new();
    private readonly IncrementingNumberGenerator _generator = new();
    
    /// <summary>
    /// The adapter under test.
    /// </summary>
    private readonly TreeFolderGeneratorCompositeItemModelAdapter<StubTreeItem, StubTreeItemFactory, EntityId, DefaultFolderModelInitializer<StubTreeItem>> _adapter;
    
    public TreeFolderGeneratorCompositeItemModelAdapterTests() {
        _sourceCache = new SourceCache<CompositeItemModel<EntityId>, EntityId>(model => model.Key);
        _adapter = new TreeFolderGeneratorCompositeItemModelAdapter<StubTreeItem, StubTreeItemFactory, EntityId, DefaultFolderModelInitializer<StubTreeItem>>
            (_factory, _sourceCache.Connect());
    }

    private CompositeItemModel<EntityId> CreateModel(EntityId key) => new(key);

    /// <summary>
    /// Tests that when a model is added to the source cache, the specific file is created
    /// in the location tree with the correct path.
    /// </summary>
    [Fact]
    public void Adapt_Add_CreateSpecificFileInLocationTree()
    {
        // Arrange
        var key = EntityId.From(1UL);
        var location = LocationId.Game;
        var path = new GamePath(location, (RelativePath)"file.txt");
        _factory.SetPath(key, path);
        var model = CreateModel(key);

        // Act
        _sourceCache.AddOrUpdate(model);

        // Assert
        _adapter.FolderGenerator.LocationIdToTree.Should().ContainKey(location);
        
        // Access the root folder of the location tree
        var locationTree = _adapter.FolderGenerator.LocationIdToTree[location];
        var rootFolder = locationTree.GetOrCreateFolder("", _generator, out _, out _);
        
        // Verify the file exists in the root folder
        rootFolder.Files.Count.Should().Be(1);
        rootFolder.Files.Items.First().Key.Should().Be(key);
    }

    /// <summary>
    /// Tests that when a model is added with a nested path, the folder structure is created correctly.
    /// </summary>
    [Fact]
    public void Adapt_Add_CreatesCorrectFolderStructure()
    {
        // Arrange
        var key = EntityId.From(5UL);
        var location = LocationId.Game;
        var path = new GamePath(location, (RelativePath)"folder1/folder2/file.txt");
        _factory.SetPath(key, path);
        var model = CreateModel(key);

        // Act
        _sourceCache.AddOrUpdate(model);

        // Assert
        _adapter.FolderGenerator.LocationIdToTree.Should().ContainKey(location);
        
        // Access the location tree
        var locationTree = _adapter.FolderGenerator.LocationIdToTree[location];

        // Get the root folder
        var rootFolder = locationTree.GetOrCreateFolder("", _generator, out _, out _);
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
        folder2.Files.Items.First().Key.Should().Be(key);
    }

    /// <summary>
    /// Tests that when the last model associated with a particular location is removed 
    /// from the source cache, the adapter properly removes the entire location tree.
    /// </summary>
    [Fact]
    public void Adapt_Remove_RemovesLocationTree_WhenLastFile()
    {
        // Arrange
        var key = EntityId.From(2UL);
        var location = LocationId.Saves;
        var path = new GamePath(location, (RelativePath)"a.txt");
        _factory.SetPath(key, path);
        var model = CreateModel(key);

        // Act - add
        _sourceCache.AddOrUpdate(model);
        // Assert - added
        _adapter.FolderGenerator.LocationIdToTree.Should().ContainKey(location);

        // Act - remove
        _sourceCache.Remove(model);

        // Assert - removed
        _adapter.FolderGenerator.LocationIdToTree.Should().NotContainKey(location);
    }

    /// <summary>
    /// Tests that when multiple models are refreshed, no duplicate files are created.
    /// </summary>
    [Fact]
    public void Adapt_Refresh_DoesNotDuplicateFiles()
    {
        // Arrange
        var key1 = EntityId.From(6UL);
        var key2 = EntityId.From(7UL);
        var location = LocationId.Game;
        
        var path1 = new GamePath(location, (RelativePath)"folder/file1.txt");
        var path2 = new GamePath(location, (RelativePath)"folder/file2.txt");
        
        _factory.SetPath(key1, path1);
        _factory.SetPath(key2, path2);
        
        var model1 = CreateModel(key1);
        var model2 = CreateModel(key2);

        // Act - add both files
        _sourceCache.AddOrUpdate(model1);
        _sourceCache.AddOrUpdate(model2);
        
        // Get the folder containing the files
        var locationTree = _adapter.FolderGenerator.LocationIdToTree[location];
        var folderPath = (RelativePath)"folder";
        var folder = locationTree.GetOrCreateFolder(folderPath, _generator, out _, out _);
        
        // Verify initial state
        folder.Files.Count.Should().Be(2);
        
        // Act - refresh all files
        _sourceCache.Refresh(model1);
        _sourceCache.Refresh(model2);

        // Assert - still only 2 files
        folder.Files.Count.Should().Be(2);
        
        // Verify each specific file exists once
        folder.Files.Items.Count(item => item.Key == key1).Should().Be(1);
        folder.Files.Items.Count(item => item.Key == key2).Should().Be(1);
    }
        
    private class StubTreeItem(GamePath path) : ITreeItemWithPath
    {
        public GamePath GetPath() => path;
    }

    private class StubTreeItemFactory : ITreeItemWithPathFactory<EntityId, StubTreeItem>
    {
        private readonly Dictionary<EntityId, GamePath> _mappings = new();
        public void SetPath(EntityId key, GamePath path) => _mappings[key] = path;
        public StubTreeItem CreateItem(EntityId key) => new(_mappings[key]);
    }
}
