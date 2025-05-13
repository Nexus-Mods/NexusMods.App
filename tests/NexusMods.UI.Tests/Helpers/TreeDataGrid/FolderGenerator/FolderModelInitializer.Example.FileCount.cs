using FluentAssertions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using DynamicData.Aggregation;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

/// <summary>
/// An example of how to use the <see cref="IFolderModelInitializer{TTreeItemWithPath}"/> for measuring file counts; 
/// i.e. data that does not require any sort of aggregation.
/// </summary>
public class FileCountAggregationTests
{
    private readonly IncrementingNumberGenerator _generator = new();
    
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
        
        var fileId1 = EntityId.From(1UL);
        var fileId2 = EntityId.From(2UL);
        var fileId3 = EntityId.From(3UL);
        var fileId4 = EntityId.From(4UL);
        
        var file1Path = (RelativePath)"folder1/file1.txt";
        var file2Path = (RelativePath)"folder1/file2.txt";
        var file3Path = (RelativePath)"folder1/subfolder/file3.txt";
        var file4Path = (RelativePath)"folder2/file4.txt";
        
        var file1Model = CreateFileModel(fileId1);
        var file2Model = CreateFileModel(fileId2);
        var file3Model = CreateFileModel(fileId3);
        var file4Model = CreateFileModel(fileId4);
        
        // Add files to the tree
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        generator.OnReceiveFile(file3Path, file3Model);
        generator.OnReceiveFile(file4Path, file4Model);
        
        // Get folder models
        var rootFolder = generator.GetOrCreateFolder("", _generator, out _, out _);
        var folder1 = generator.GetOrCreateFolder("folder1", _generator, out _, out _);
        var subfolder = generator.GetOrCreateFolder("folder1/subfolder", _generator, out _, out _);
        var folder2 = generator.GetOrCreateFolder("folder2", _generator, out _, out _);
        
        // Act - verify file counts
        
        // Assert
        // The FileCount component should be populated with the correct counts
        rootFolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(4); // All 4 files
        folder1.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(3);    // file1, file2, file3
        subfolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);  // file3
        folder2.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);    // file4
    }
    
    [Fact]
    public void FileCountInitializer_ShouldUpdateCountWhenFilesAdded()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Create initial structure with one file
        var fileId1 = EntityId.From(1UL);
        var file1Path = (RelativePath)"folder1/file1.txt";
        var file1Model = CreateFileModel(fileId1);
        
        generator.OnReceiveFile(file1Path, file1Model);
        
        var rootFolder = generator.GetOrCreateFolder("", _generator, out _, out _);
        var folder1 = generator.GetOrCreateFolder("folder1", _generator, out _, out _);
        
        // Initial counts
        rootFolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);
        folder1.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);
        
        // Act - Add another file
        var fileId2 = EntityId.From(2UL);
        var file2Path = (RelativePath)"folder1/subfolder/file2.txt";
        var file2Model = CreateFileModel(fileId2);
        
        generator.OnReceiveFile(file2Path, file2Model);
        
        // Get reference to the new subfolder
        var subfolder = generator.GetOrCreateFolder("folder1/subfolder", _generator, out _, out _);
        
        // Assert - Counts should be updated
        rootFolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(2);
        folder1.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(2);
        subfolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);
    }
    
    [Fact]
    public void FileCountInitializer_ShouldUpdateCountWhenFilesRemoved()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Setup test structure
        var fileId1 = EntityId.From(1UL);
        var fileId2 = EntityId.From(2UL);
        
        var file1Path = (RelativePath)"folder1/file1.txt";
        var file2Path = (RelativePath)"folder1/subfolder/file2.txt";
        
        var file1Model = CreateFileModel(fileId1);
        var file2Model = CreateFileModel(fileId2);
        
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        
        var rootFolder = generator.GetOrCreateFolder("", _generator, out _, out _);
        var folder1 = generator.GetOrCreateFolder("folder1", _generator, out _, out _);
        var subfolder = generator.GetOrCreateFolder("folder1/subfolder", _generator, out _, out _);
        
        // Verify initial counts
        rootFolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(2);
        folder1.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(2);
        subfolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);
        
        // Act - Remove a file
        generator.OnDeleteFile(file2Path, file2Model);
        
        // Assert - Counts should be updated
        rootFolder.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);
        folder1.Model.Get<ValueComponent<int>>(FileCountComponentKey.Key).Value.Value.Should().Be(1);
        
        // subfolder should have been removed since it's empty
        var folderExists = folder1.Folders.Lookup("subfolder").HasValue;
        folderExists.Should().BeFalse();
    }
    
    // Helper methods
    private TreeFolderGeneratorForLocationId<ITreeItemWithPath, FileCountFolderModelInitializer> CreateGenerator()
    {
        return new TreeFolderGeneratorForLocationId<ITreeItemWithPath, FileCountFolderModelInitializer>("", _generator);
    }
    
    private static CompositeItemModel<EntityId> CreateFileModel(EntityId id)
    {
        return new CompositeItemModel<EntityId>(id);
    }
}

public static class FileCountComponentKey
{
    public static readonly ComponentKey Key = ComponentKey.From("FileCount");
}

/// <summary>
/// A custom folder model initializer that adds a FileCount component to track the number of files
/// recursively within a folder.
/// </summary>
public class FileCountFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
{
    /// <inheritdoc/>
    public static void InitializeModel<TFolderModelInitializer>(
        CompositeItemModel<EntityId> model,
        GeneratedFolder<ITreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
    {
        // Create an observable of file counts from the recursive file list
        var fileCountObservable = folder.GetAllFilesRecursiveObservable()
            .Count(); // Note(sewer): This is DynamicData's Count. Not Reactive's !!

        // Add a ValueComponent that will update automatically when the observed count changes
        var component = new ValueComponent<int>(
            initialValue: 0,
            valueObservable: fileCountObservable,
            subscribeWhenCreated: true,
            observeOutsideUiThread: true
        );
        model.Add(FileCountComponentKey.Key, component);
    }
}
