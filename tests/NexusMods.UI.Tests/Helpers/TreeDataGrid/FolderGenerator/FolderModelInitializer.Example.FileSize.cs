using System.Reactive.Linq;
using FluentAssertions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using DynamicData.Aggregation;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

/// <summary>
/// An example of how to use the <see cref="IFolderModelInitializer{TTreeItemWithPath}"/> for measuring file counts; 
/// i.e. data that does not require any sort of aggregation.
/// </summary>
public class FileSizeAggregationTests
{
#pragma warning disable CA2211
    public static ComponentKey ComponentKey = ComponentKey.From("FileSizeAggregationTestsKey");
#pragma warning restore CA2211
    
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
        
        var locationId = LocationId.From(1);

        var file1Path = new GamePath(locationId, "folder1/file1.txt");
        var file2Path = new GamePath(locationId, "folder1/file2.txt");
        var file3Path = new GamePath(locationId, "folder1/subfolder/file3.txt");
        var file4Path = new GamePath(locationId, "folder2/file4.txt");

        var file1Model = CreateFileModel(file1Path, 100);
        var file2Model = CreateFileModel(file2Path, 200);
        var file3Model = CreateFileModel(file3Path, 300);
        var file4Model = CreateFileModel(file4Path, 400);

        // Add files to the tree
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        generator.OnReceiveFile(file3Path, file3Model);
        generator.OnReceiveFile(file4Path, file4Model);

        // Get folder models
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _, out _);
        var folder1 = generator.GetOrCreateFolder(new GamePath(locationId, "folder1"), out _, out _);
        var subfolder = generator.GetOrCreateFolder(new GamePath(locationId, "folder1/subfolder"), out _, out _);
        var folder2 = generator.GetOrCreateFolder(new GamePath(locationId, "folder2"), out _, out _);
        
        // Act - verify file sizes
        
        // Assert
        // The FileSize component should be populated with the correct total sizes
        rootFolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(1000); // All files (100+200+300+400)
        folder1.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(600);     // file1+file2+file3 (100+200+300)
        subfolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(300);   // file3 (300)
        folder2.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(400);     // file4 (400)
    }
    
    [Fact]
    public void FileSizeInitializer_ShouldUpdateSizeWhenFilesAdded()
    {
        // Arrange
        var generator = CreateGenerator();
        var locationId = LocationId.From(1);
        
        // Create initial structure with one file
        var file1Path = new GamePath(locationId, "folder1/file1.txt");
        var file1Model = CreateFileModel(file1Path, 100);
        
        generator.OnReceiveFile(file1Path, file1Model);
        
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _, out _);
        var folder1 = generator.GetOrCreateFolder(new GamePath(locationId, "folder1"), out _, out _);
        
        // Initial sizes
        rootFolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(100);
        folder1.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(100);
        
        // Act - Add another file
        var file2Path = new GamePath(locationId, "folder1/subfolder/file2.txt");
        var file2Model = CreateFileModel(file2Path, 200);
        
        generator.OnReceiveFile(file2Path, file2Model);
        
        // Get reference to the new subfolder
        var subfolder = generator.GetOrCreateFolder(new GamePath(locationId, "folder1/subfolder"), out _, out _);
        
        // Assert - Sizes should be updated
        rootFolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(300); // 100 + 200
        folder1.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(300);    // 100 + 200
        subfolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(200);  // 200
    }
    
    [Fact]
    public void FileSizeInitializer_ShouldUpdateSizeWhenFilesRemoved()
    {
        // Arrange
        var generator = CreateGenerator();
        var locationId = LocationId.From(1);
        
        // Setup test structure
        var file1Path = new GamePath(locationId, "folder1/file1.txt");
        var file2Path = new GamePath(locationId, "folder1/subfolder/file2.txt");
        
        var file1Model = CreateFileModel(file1Path, 100);
        var file2Model = CreateFileModel(file2Path, 200);
        
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _, out _);
        var folder1 = generator.GetOrCreateFolder(new GamePath(locationId, "folder1"), out _, out _);
        var subfolder = generator.GetOrCreateFolder(new GamePath(locationId, "folder1/subfolder"), out _, out _);
        
        // Verify initial sizes
        rootFolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(300); // 100 + 200
        folder1.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(300);    // 100 + 200
        subfolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(200);  // 200
        
        // Act - Remove a file
        generator.OnDeleteFile(file2Path, file2Model);
        
        // Assert - Sizes should be updated
        rootFolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(100); // Only file1 remains
        folder1.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(100);    // Only file1 remains
        
        // subfolder should have been removed since it's empty
        var folderExists = folder1.Folders.Lookup(new GamePath(locationId, "folder1/subfolder")).HasValue;
        folderExists.Should().BeFalse();
    }
    
    [Fact]
    public void FileSizeInitializer_ShouldHandleZeroSizeFiles()
    {
        // Arrange
        var generator = CreateGenerator();

        var locationId = LocationId.From(1);
        var file1Path = new GamePath(locationId, "folder1/empty.txt");
        var file2Path = new GamePath(locationId, "folder1/nonempty.txt");
        
        var file1Model = CreateFileModel(file1Path, 0);         // Empty file
        var file2Model = CreateFileModel(file2Path, 100);       // Non-empty file
        
        generator.OnReceiveFile(file1Path, file1Model);
        generator.OnReceiveFile(file2Path, file2Model);
        
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _, out _);
        var folder1 = generator.GetOrCreateFolder(new GamePath(locationId, "folder1"), out _, out _);
        
        // Assert
        rootFolder.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(100); // Only the non-empty file contributes
        folder1.Model.Get<SizeComponent>(ComponentKey).Value.Value.Value.Should().Be(100);    // Only the non-empty file contributes
    }
    
    // Helper methods
    private TreeFolderGeneratorForLocationId<GamePathTreeItemWithPath, FileSizeFolderModelInitializer> CreateGenerator()
    {
        return new TreeFolderGeneratorForLocationId<GamePathTreeItemWithPath, FileSizeFolderModelInitializer>(GamePath.Empty(LocationId.From(1)));
    }

    private static CompositeItemModel<GamePath> CreateFileModel(GamePath gamePath, long fileSize)
    {
        var model = new CompositeItemModel<GamePath>(gamePath);
        model.Add(ComponentKey, new SizeComponent(Size.FromLong(fileSize)));
        return model;
    }
}

/// <summary>
/// A custom folder model initializer that adds a FileSize component to track the total size
/// of all files recursively within a folder.
/// </summary>
public class FileSizeFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
{
    /// <inheritdoc/>
    public static void InitializeModel<TFolderModelInitializer>(CompositeItemModel<GamePath> model, GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder) where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
        // Create an observable that transforms the file items to their sizes then sums them
        var fileSizeObservable = folder.GetAllFilesRecursiveObservable()
            .Transform(fileModel => fileModel.TryGet<SizeComponent>(FileSizeAggregationTests.ComponentKey, out var sizeComponent) ? (long)sizeComponent.Value.Value.Value : 0L)
            .Sum(x => x) // Note(sewer): dynamicdata summation lacks unsigned. But we're talking 64-bit, good luck reaching >8 exabytes on a mod.
            .Select(x => Size.From((ulong)x)); // Sum up all the sizes
        
        // Add a ValueComponent that will update automatically when the observed total size changes
        var component = new SizeComponent(
            initialValue: Size.Zero,
            valueObservable: fileSizeObservable,
            subscribeWhenCreated: true
        );
        model.Add(FileSizeAggregationTests.ComponentKey, component);
    }
}
