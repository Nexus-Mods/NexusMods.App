using System.Reactive.Linq;
using FluentAssertions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using DynamicData.Aggregation;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

/// <summary>
/// An example of how to use the <see cref="IFolderModelInitializer{TTreeItemWithPath}"/> for measuring file counts; 
/// i.e. data that does not require any sort of aggregation.
/// </summary>
public class FileCountAggregationTests
{
#pragma warning disable CA2211
    public static readonly ComponentKey ComponentKey = ComponentKey.From("FileCount"); 
#pragma warning restore CA2211

    [Fact]
    public void FileCountInitializer_ShouldTrackRecursiveFileCount()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Setup test data structure
        // Root
        // ├── folder1
        // │   ├── file1.txt
        // │   ├── file2.txt
        // │   └── subfolder
        // │       └── file3.txt
        // └── folder2
        //     └── file4.txt
        
        var locationId = LocationId.From(1);
        
        var file1Path = new GamePath(locationId, "folder1/file1.txt");
        var file2Path = new GamePath(locationId, "folder1/file2.txt");
        var file3Path = new GamePath(locationId, "folder1/subfolder/file3.txt");
        var file4Path = new GamePath(locationId, "folder2/file4.txt");
        
        var file1Model = CreateFileModel(file1Path);
        var file2Model = CreateFileModel(file2Path);
        var file3Model = CreateFileModel(file3Path);
        var file4Model = CreateFileModel(file4Path);
        
        // Add files to the tree
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        generator.OnReceiveFile(file3Path, file3Model);
        generator.OnReceiveFile(file4Path, file4Model);
        
        // Get folder models
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        var folder1 = generator.GetOrCreateFolder(new GamePath(locationId, "folder1"), out _);
        var subfolder = generator.GetOrCreateFolder(new GamePath(locationId, "folder1/subfolder"), out _);
        var folder2 = generator.GetOrCreateFolder(new GamePath(locationId, "folder2"), out _);
        
        // Act - verify file counts
        
        // Assert
        // The FileCount component should be populated with the correct counts
        rootFolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(4); // All 4 files
        folder1.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(3);    // file1, file2, file3
        subfolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);  // file3
        folder2.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);    // file4
    }
    
    [Fact]
    public void FileCountInitializer_ShouldUpdateCountWhenFilesAdded()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Create initial structure with one file
        var locationId = LocationId.From(1);
        var file1Path = new GamePath(locationId, "folder1/file1.txt");
        var file1Model = CreateFileModel(file1Path);
        
        generator.OnReceiveFile(file1Path, file1Model);
        
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        var folder1 = generator.GetOrCreateFolder(new GamePath(locationId, "folder1"), out _);
        
        // Initial counts
        rootFolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);
        folder1.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);
        
        // Act - Add another file
        var file2Path = new GamePath(locationId, "folder1/subfolder/file2.txt");
        var file2Model = CreateFileModel(file2Path);
        
        generator.OnReceiveFile(file2Path, file2Model);
        
        // Get reference to the new subfolder
        var subfolder = generator.GetOrCreateFolder(new GamePath(locationId, "folder1/subfolder"), out _);
        
        // Assert - Counts should be updated
        rootFolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(2);
        folder1.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(2);
        subfolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);
    }
    
    [Fact]
    public void FileCountInitializer_ShouldUpdateCountWhenFilesRemoved()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Setup test structure
        var locationId = LocationId.From(1);
        
        var file1Path = new GamePath(locationId, "folder1/file1.txt");
        var file2Path = new GamePath(locationId, "folder1/subfolder/file2.txt");
        
        var file1Model = CreateFileModel(file1Path);
        var file2Model = CreateFileModel(file2Path);
        
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        var folder1 = generator.GetOrCreateFolder(new GamePath(locationId, "folder1"), out _);
        var subfolder = generator.GetOrCreateFolder(new GamePath(locationId, "folder1/subfolder"), out _);
        
        // Verify initial counts
        rootFolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(2);
        folder1.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(2);
        subfolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);
        
        // Act - Remove a file
        generator.OnDeleteFile(file2Path, file2Model);
        
        // Assert - Counts should be updated
        rootFolder.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);
        folder1.Model.Get<UInt32Component>(ComponentKey).Value.Value.Should().Be(1);
        
        // subfolder should have been removed since it's empty
        var folderExists = folder1.Folders.Lookup(new GamePath(locationId, "folder1/subfolder")).HasValue;
        folderExists.Should().BeFalse();
    }
    
    // Helper methods
    private TreeFolderGeneratorForLocationId<GamePathTreeItemWithPath, FileCountFolderModelInitializer> CreateGenerator()
    {
        return new TreeFolderGeneratorForLocationId<GamePathTreeItemWithPath, FileCountFolderModelInitializer>(GamePath.Empty(LocationId.From(1)));
    }
    
    private static CompositeItemModel<GamePath> CreateFileModel(GamePath gamePath)
    {
        return new CompositeItemModel<GamePath>(gamePath);
    }
}

/// <summary>
/// A custom folder model initializer that adds a FileCount component to track the number of files
/// recursively within a folder.
/// </summary>
public class FileCountFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
{
    /// <inheritdoc/>
    public static void InitializeModel<TFolderModelInitializer>(
        CompositeItemModel<GamePath> model,
        GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
        var fileCountObservable = folder.GetAllFilesRecursiveObservable()
            .Count() // Note(sewer): This is DynamicData's Count. Not Reactive's !!
            .Select(x => (uint)x);

        var component = new UInt32Component(
            initialValue: 0,
            valueObservable: fileCountObservable,
            subscribeWhenCreated: true,
            observeOutsideUiThread: true
        );
        model.Add(FileCountAggregationTests.ComponentKey, component);
    }
}
