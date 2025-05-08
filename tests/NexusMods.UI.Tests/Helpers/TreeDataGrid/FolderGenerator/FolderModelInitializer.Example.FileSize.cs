using FluentAssertions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using DynamicData.Aggregation;
using System.Reactive.Linq;
using DynamicData;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

/// <summary>
/// An example of how to use the <see cref="IFolderModelInitializer{TTreeItemWithPath}"/> for measuring file counts; 
/// i.e. data that does not require any sort of aggregation.
/// </summary>
public class FileSizeAggregationTests
{
    private readonly IncrementingNumberGenerator _generator = new();
    
    [Fact]
    public void FileSizeInitializer_ShouldTrackRecursiveFileSize()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Setup test data structure
        // Root
        // ├── folder1
        // │   ├── file1.txt (100 bytes)
        // │   ├── file2.txt (200 bytes)
        // │   └── subfolder
        // │       └── file3.txt (300 bytes)
        // └── folder2
        //     └── file4.txt (400 bytes)
        
        var fileId1 = EntityId.From(1UL);
        var fileId2 = EntityId.From(2UL);
        var fileId3 = EntityId.From(3UL);
        var fileId4 = EntityId.From(4UL);
        
        var file1Path = (RelativePath)"folder1/file1.txt";
        var file2Path = (RelativePath)"folder1/file2.txt";
        var file3Path = (RelativePath)"folder1/subfolder/file3.txt";
        var file4Path = (RelativePath)"folder2/file4.txt";
        
        var file1Model = CreateFileModel(fileId1, 100);
        var file2Model = CreateFileModel(fileId2, 200);
        var file3Model = CreateFileModel(fileId3, 300);
        var file4Model = CreateFileModel(fileId4, 400);
        
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
        
        // Act - verify file sizes
        
        // Assert
        // The FileSize component should be populated with the correct total sizes
        rootFolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(1000); // All files (100+200+300+400)
        folder1.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(600);     // file1+file2+file3 (100+200+300)
        subfolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(300);   // file3 (300)
        folder2.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(400);     // file4 (400)
    }
    
    [Fact]
    public void FileSizeInitializer_ShouldUpdateSizeWhenFilesAdded()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Create initial structure with one file
        var fileId1 = EntityId.From(1UL);
        var file1Path = (RelativePath)"folder1/file1.txt";
        var file1Model = CreateFileModel(fileId1, 100);
        
        generator.OnReceiveFile(file1Path, file1Model);
        
        var rootFolder = generator.GetOrCreateFolder("", _generator, out _, out _);
        var folder1 = generator.GetOrCreateFolder("folder1", _generator, out _, out _);
        
        // Initial sizes
        rootFolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(100);
        folder1.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(100);
        
        // Act - Add another file
        var fileId2 = EntityId.From(2UL);
        var file2Path = (RelativePath)"folder1/subfolder/file2.txt";
        var file2Model = CreateFileModel(fileId2, 200);
        
        generator.OnReceiveFile(file2Path, file2Model);
        
        // Get reference to the new subfolder
        var subfolder = generator.GetOrCreateFolder("folder1/subfolder", _generator, out _, out _);
        
        // Assert - Sizes should be updated
        rootFolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(300); // 100 + 200
        folder1.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(300);    // 100 + 200
        subfolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(200);  // 200
    }
    
    [Fact]
    public void FileSizeInitializer_ShouldUpdateSizeWhenFilesRemoved()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Setup test structure
        var fileId1 = EntityId.From(1UL);
        var fileId2 = EntityId.From(2UL);
        
        var file1Path = (RelativePath)"folder1/file1.txt";
        var file2Path = (RelativePath)"folder1/subfolder/file2.txt";
        
        var file1Model = CreateFileModel(fileId1, 100);
        var file2Model = CreateFileModel(fileId2, 200);
        
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        
        var rootFolder = generator.GetOrCreateFolder("", _generator, out _, out _);
        var folder1 = generator.GetOrCreateFolder("folder1", _generator, out _, out _);
        var subfolder = generator.GetOrCreateFolder("folder1/subfolder", _generator, out _, out _);
        
        // Verify initial sizes
        rootFolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(300); // 100 + 200
        folder1.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(300);    // 100 + 200
        subfolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(200);  // 200
        
        // Act - Remove a file
        generator.OnDeleteFile(file2Path, file2Model);
        
        // Assert - Sizes should be updated
        rootFolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(100); // Only file1 remains
        folder1.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(100);    // Only file1 remains
        
        // subfolder should have been removed since it's empty
        var folderExists = folder1.Folders.Lookup("subfolder").HasValue;
        folderExists.Should().BeFalse();
    }
    
    [Fact]
    public void FileSizeInitializer_ShouldHandleZeroSizeFiles()
    {
        // Arrange
        var generator = CreateGenerator();
        
        var fileId1 = EntityId.From(1UL);
        var fileId2 = EntityId.From(2UL);
        
        var file1Path = (RelativePath)"folder1/empty.txt";
        var file2Path = (RelativePath)"folder1/nonempty.txt";
        
        var file1Model = CreateFileModel(fileId1, 0);         // Empty file
        var file2Model = CreateFileModel(fileId2, 100);       // Non-empty file
        
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        
        var rootFolder = generator.GetOrCreateFolder("", _generator, out _, out _);
        var folder1 = generator.GetOrCreateFolder("folder1", _generator, out _, out _);
        
        // Assert
        rootFolder.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(100); // Only the non-empty file contributes
        folder1.Model.Get<ValueComponent<long>>(FileSizeComponentKey.Key).Value.Value.Should().Be(100);    // Only the non-empty file contributes
    }
    
    // Helper methods
    private TreeFolderGeneratorForLocationId<ITreeItemWithPath, FileSizeFolderModelInitializer> CreateGenerator()
    {
        return new TreeFolderGeneratorForLocationId<ITreeItemWithPath, FileSizeFolderModelInitializer>("", _generator);
    }
    
    private static CompositeItemModel<EntityId> CreateFileModel(EntityId id, long fileSize)
    {
        var model = new CompositeItemModel<EntityId>(id);
        model.Add(FileSizeComponentKey.Key, new ValueComponent<long>(fileSize));
        return model;
    }
}

/// <summary>
/// Component key for the file size component
/// </summary>
public static class FileSizeComponentKey
{
    public static readonly ComponentKey Key = ComponentKey.From("FileSize");
}

/// <summary>
/// A custom folder model initializer that adds a FileSize component to track the total size
/// of all files recursively within a folder.
/// </summary>
public class FileSizeFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
{
    /// <inheritdoc/>
    public static void InitializeModel<TFolderModelInitializer>(
        CompositeItemModel<EntityId> model,
        GeneratedFolder<ITreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
    {
        // Create an observable that transforms the file items to their sizes then sums them
        var fileSizeObservable = folder.GetAllFilesRecursiveObservable()
            .Transform(fileModel => fileModel.TryGet<ValueComponent<long>>(FileSizeComponentKey.Key, out var sizeComponent) ? sizeComponent.Value.Value : 0L)
            .Sum(x => x); // Sum up all the sizes
        
        // Add a ValueComponent that will update automatically when the observed total size changes
        var component = new ValueComponent<long>(
            initialValue: 0,
            valueObservable: fileSizeObservable,
            subscribeWhenCreated: true,
            observeOutsideUiThread: true
        );
        model.Add(FileSizeComponentKey.Key, component);
    }
}
