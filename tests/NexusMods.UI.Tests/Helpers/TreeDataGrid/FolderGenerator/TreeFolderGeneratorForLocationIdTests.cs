#pragma warning disable CS0618 // Type or member is obsolete. Uses internal APIs for testing only.
using FluentAssertions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using System.Collections.ObjectModel;
using DynamicData;

namespace NexusMods.UI.Tests.Helpers.TreeDataGrid.FolderGenerator;

public class TreeFolderGeneratorForLocationIdTests
{
    [Fact]
    public void OnReceiveFile_AddsFileToRoot_WhenPathHasNoParent()
    {
        // Arrange  
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("file.txt");
        var fileModel = CreateFileModel(fileId);

        var generator = CreateGenerator();

        // Act
        generator.OnReceiveFile(filePath, fileModel);

        // Assert
        var rootFolder = generator.GetOrCreateFolder("", out _, out _);
        rootFolder.Files.Lookup(fileId).HasValue.Should().BeTrue();
        rootFolder.Files.Lookup(fileId).Value.Should().Be(fileModel);
        rootFolder.Files.Count.Should().Be(1);
        rootFolder.Folders.Count.Should().Be(0);
    }

    [Fact]
    public void OnReceiveFile_AddsFileToNestedFolder_WhenPathHasParents()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("folder1/folder2/file.txt");
        var fileModel = CreateFileModel(fileId);
        
        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert
        var rootFolder = generator.GetOrCreateFolder("", out _, out _);
        rootFolder.Files.Count.Should().Be(0);
        rootFolder.Folders.Count.Should().Be(1);

        // Navigate to folder1
        var folder1 = rootFolder.Folders.Lookup("folder1").Value;
        folder1.Files.Count.Should().Be(0);
        folder1.Folders.Count.Should().Be(1);
        
        // Navigate to folder2
        var folder2 = folder1.Folders.Lookup("folder2").Value;
        folder2.Files.Count.Should().Be(1);
        folder2.Files.Lookup(fileId).HasValue.Should().BeTrue();
        folder2.Files.Lookup(fileId).Value.Should().Be(fileModel);
    }
    
    [Fact]
    public void OnReceiveFile_IncrementsRefCount_OnlyForNewFiles()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("folder/file.txt");
        var fileModel = CreateFileModel(fileId);
        
        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath, fileModel);
        
        // Get folder and check initial refcount
        var folder = generator.GetOrCreateFolder("folder", out _, out _);
        folder.Files.Count.Should().Be(1);

        // Act again with same file - should not increment refcount
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert
        folder.Files.Count.Should().Be(1);
        
        // Act again with different file - should increment refcount
        var fileId2 = EntityId.From(1UL);
        var fileModel2 = CreateFileModel(fileId2);
        generator.OnReceiveFile(filePath.Parent.Join("file2.txt"), fileModel2);
        
        // Assert
        folder.Files.Count.Should().Be(2);
    }
    
    [Fact]
    public void OnDeleteFile_RemovesFile_AndReturnsTrueWhenFolderEmpty()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("folder/file.txt");
        var fileModel = CreateFileModel(fileId);
        
        // Setup
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath, fileModel);

        // Act
        var result = generator.OnDeleteFile(filePath, fileModel);
        
        // Assert
        result.Should().BeTrue(); // Folder should be empty now
        var rootFolder = generator.GetOrCreateFolder("", out _, out _);
        rootFolder.Folders.Count.Should().Be(0); // Folder should be removed
    }
    
    [Fact]
    public void OnDeleteFile_RemovesFile_AndReturnsFalseWhenFolderNotEmpty()
    {
        // Arrange
        var fileId1 = EntityId.From(0UL);
        var fileId2 = EntityId.From(1UL);
        var filePath1 = new RelativePath("folder/file1.txt");
        var filePath2 = new RelativePath("folder/file2.txt");
        var fileModel1 = CreateFileModel(fileId1);
        var fileModel2 = CreateFileModel(fileId2);
        
        // Setup
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath1, fileModel1);
        generator.OnReceiveFile(filePath2, fileModel2);
        
        // Act
        var result = generator.OnDeleteFile(filePath1, fileModel1);
        
        // Assert
        result.Should().BeFalse(); // Folder should not be empty
        var folder = generator.GetOrCreateFolder("folder", out _, out _);
        folder.Files.Count.Should().Be(1);
        folder.Files.Lookup(fileId1).HasValue.Should().BeFalse();
        folder.Files.Lookup(fileId2).HasValue.Should().BeTrue();
    }
    
    [Fact]
    public void GetOrCreateFolder_CreatesNestedFolders_WhenTheyDontExist()
    {
        // Arrange
        var path = new RelativePath("folder1/folder2/folder3");
        
        // Act
        var generator = CreateGenerator();
        var folder = generator.GetOrCreateFolder(path, out var parentFolder, out var parentFolderName);
        
        // Assert
        folder.Should().NotBeNull();
        parentFolder.Should().NotBeNull();
        parentFolderName.Should().Be("folder3");
        
        // Navigate from root to confirm structure
        var rootFolder = generator.GetOrCreateFolder("", out _, out _);
        rootFolder.Folders.Count.Should().Be(1);
        
        var folder1 = rootFolder.Folders.Lookup("folder1").Value;
        folder1.Folders.Count.Should().Be(1);
        
        var folder2 = folder1.Folders.Lookup("folder2").Value;
        folder2.Folders.Count.Should().Be(1);
        
        var folder3 = folder2.Folders.Lookup("folder3").Value;
        folder3.Should().BeSameAs(folder);
    }
    
    [Fact]
    public void GetOrCreateFolder_ReturnsExistingFolder_WhenItExists()
    {
        // Arrange
        var path = new RelativePath("folder1/folder2");
        
        // Act - create folder first time
        var generator = CreateGenerator();
        var initialFolder = generator.GetOrCreateFolder(path, out _, out _);
        
        // Act - get the same folder again
        var retrievedFolder = generator.GetOrCreateFolder(path, out var parentFolder, out var parentFolderName);
        
        // Assert
        retrievedFolder.Should().BeSameAs(initialFolder);
        parentFolder.Should().NotBeNull();
        parentFolderName.Should().Be("folder2");
    }
    
    [Fact]
    public void ModelForRoot_ReturnsRootFolderModel()
    {
        // Arrange
        var generator = CreateGenerator();
        
        // Act
        var model = generator.ModelForRoot();
        
        // Assert
        model.Should().NotBeNull();
        model.Should().BeSameAs(generator.GetOrCreateFolder("", out _, out _).Model);
    }
    
    // Observable-specific tests

    [Fact]
    public void RootObservables_InitializeCorrectly_WhenEmpty()
    {
        // Arrange
        var generator = CreateGenerator();
        var hasChildren = false;
        
        // Act
        using var hasChildrenSub = generator.ModelForRoot().GetHasChildrenObservable_ForTestingOnly().Subscribe(x => hasChildren = x);
        using var children = BindChildren(generator.ModelForRoot().GetChildrenObservable_ForTestingOnly(), out var childrenCollection);
        
        // Assert
        hasChildren.Should().BeFalse();
        childrenCollection.Should().BeEmpty();
    }
    
    [Fact]
    public void RootObservables_UpdateCorrectly_WhenFileAddedToRoot()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("file.txt");
        var fileModel = CreateFileModel(fileId);
        var generator = CreateGenerator();
        var hasChildren = false;
        
        using var hasChildrenSub = generator.ModelForRoot().GetHasChildrenObservable_ForTestingOnly().Subscribe(x => hasChildren = x);
        using var children = BindChildren(generator.ModelForRoot().GetChildrenObservable_ForTestingOnly(), out var childrenCollection);
        
        // Assert initial state
        hasChildren.Should().BeFalse();
        childrenCollection.Should().BeEmpty();
        
        // Act
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert
        hasChildren.Should().BeTrue();
        childrenCollection.Should().ContainSingle().Which.Should().Be(fileModel);
        
        // Act - remove file
        generator.OnDeleteFile(filePath, fileModel);
        
        // Assert
        hasChildren.Should().BeFalse();
        childrenCollection.Should().BeEmpty();
    }
    
    [Fact]
    public void RootObservables_UpdateCorrectly_WhenFileAddedToFolder()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("folder/file.txt");
        var fileModel = CreateFileModel(fileId);
        var generator = CreateGenerator();
        var rootHasChildren = false;
        
        using var rootHasChildrenSub = generator.ModelForRoot().GetHasChildrenObservable_ForTestingOnly().Subscribe(x => rootHasChildren = x);
        using var rootChildren = BindChildren(generator.ModelForRoot().GetChildrenObservable_ForTestingOnly(), out var rootChildrenCollection);
        
        // Assert initial state
        rootHasChildren.Should().BeFalse();
        rootChildrenCollection.Should().BeEmpty();
        
        // Act
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert root observables
        rootHasChildren.Should().BeTrue();
        rootChildrenCollection.Should().ContainSingle(); // Should contain the folder
        
        // Get folder and check its observables
        var folder = generator.GetOrCreateFolder("folder", out _, out _);
        var folderHasChildren = false;
        using var folderHasChildrenSub = folder.Model.GetHasChildrenObservable_ForTestingOnly().Subscribe(x => folderHasChildren = x);
        using var folderChildren = BindChildren(folder.Model.GetChildrenObservable_ForTestingOnly(), out var folderChildrenCollection);
        
        folderHasChildren.Should().BeTrue();
        folderChildrenCollection.Should().ContainSingle().Which.Should().Be(fileModel);
        
        // Act - remove file
        generator.OnDeleteFile(filePath, fileModel);
        
        // Assert root observables after deletion
        rootHasChildren.Should().BeFalse();
        rootChildrenCollection.Should().BeEmpty();
    }
    
    [Fact]
    public void FolderObservables_UpdateCorrectly_WhenMultipleFilesAddedAndRemoved()
    {
        // Arrange
        var fileId1 = EntityId.From(0UL);
        var fileId2 = EntityId.From(1UL);
        var filePath1 = new RelativePath("folder/file1.txt");
        var filePath2 = new RelativePath("folder/file2.txt");
        var fileModel1 = CreateFileModel(fileId1);
        var fileModel2 = CreateFileModel(fileId2);
        var generator = CreateGenerator();
        var rootHasChildren = false;
        
        using var rootHasChildrenSub = generator.ModelForRoot().GetHasChildrenObservable_ForTestingOnly().Subscribe(x => rootHasChildren = x);
        using var rootChildren = BindChildren(generator.ModelForRoot().GetChildrenObservable_ForTestingOnly(), out var rootChildrenCollection);
        
        // Assert initial state
        rootHasChildren.Should().BeFalse();
        rootChildrenCollection.Should().BeEmpty();
        
        // Act - Add files to folder
        generator.OnReceiveFile(filePath1, fileModel1);
        generator.OnReceiveFile(filePath2, fileModel2);
        
        // Assert root observables
        rootHasChildren.Should().BeTrue();
        rootChildrenCollection.Should().ContainSingle(); // Contains folder
        
        // Get folder and bind observables
        var folder = generator.GetOrCreateFolder("folder", out _, out _);
        var folderHasChildren = false;
        using var folderHasChildrenSub = folder.Model.GetHasChildrenObservable_ForTestingOnly().Subscribe(x => folderHasChildren = x);
        using var folderChildren = BindChildren(folder.Model.GetChildrenObservable_ForTestingOnly(), out var folderChildrenCollection);
        
        // Assert folder observables
        folderHasChildren.Should().BeTrue();
        folderChildrenCollection.Should().HaveCount(2);
        folderChildrenCollection.Should().Contain(fileModel1);
        folderChildrenCollection.Should().Contain(fileModel2);
        
        // Act - Remove one file
        generator.OnDeleteFile(filePath1, fileModel1);
        
        // Assert observables after first deletion
        rootHasChildren.Should().BeTrue(); // Root still has folder
        rootChildrenCollection.Should().ContainSingle(); // Still contains folder
        folderHasChildren.Should().BeTrue(); // Folder still has one file
        folderChildrenCollection.Should().ContainSingle().Which.Should().Be(fileModel2);
        
        // Act - Remove the other file
        generator.OnDeleteFile(filePath2, fileModel2);
        
        // Assert observables after second deletion
        rootHasChildren.Should().BeFalse(); // Root has no children
        rootChildrenCollection.Should().BeEmpty(); // No folders left
    }
    
    [Fact]
    public void NestedFolderObservables_UpdateCorrectly_WhenFileAddedAndRemoved()
    {
        // Arrange
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("folder1/folder2/folder3/file.txt");
        var fileModel = CreateFileModel(fileId);
        var generator = CreateGenerator();
        var rootHasChildren = false;
        
        using var rootHasChildrenSub = generator.ModelForRoot().GetHasChildrenObservable_ForTestingOnly().Subscribe(x => rootHasChildren = x);
        using var rootChildren = BindChildren(generator.ModelForRoot().GetChildrenObservable_ForTestingOnly(), out var rootChildrenCollection);
        
        // Assert initial state
        rootHasChildren.Should().BeFalse();
        rootChildrenCollection.Should().BeEmpty();
        
        // Act - Add file to deeply nested folder
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert root observables
        rootHasChildren.Should().BeTrue();
        rootChildrenCollection.Should().ContainSingle(); // Contains folder1
        
        // Get all folders
        var rootFolder = generator.GetOrCreateFolder("", out _, out _);
        var folder1 = rootFolder.Folders.Lookup("folder1").Value;
        var folder2 = folder1.Folders.Lookup("folder2").Value;
        var folder3 = folder2.Folders.Lookup("folder3").Value;
        
        // Check folder1 observables
        var folder1HasChildren = false;
        using var folder1HasChildrenSub = folder1.Model.GetHasChildrenObservable_ForTestingOnly().Subscribe(x => folder1HasChildren = x);
        using var folder1Children = BindChildren(folder1.Model.GetChildrenObservable_ForTestingOnly(), out var folder1ChildrenCollection);
        folder1HasChildren.Should().BeTrue();
        folder1ChildrenCollection.Should().ContainSingle(); // Contains folder2
        
        // Check folder2 observables
        var folder2HasChildren = false;
        using var folder2HasChildrenSub = folder2.Model.GetHasChildrenObservable_ForTestingOnly().Subscribe(x => folder2HasChildren = x);
        using var folder2Children = BindChildren(folder2.Model.GetChildrenObservable_ForTestingOnly(), out var folder2ChildrenCollection);
        folder2HasChildren.Should().BeTrue();
        folder2ChildrenCollection.Should().ContainSingle(); // Contains folder3
        
        // Check folder3 observables
        var folder3HasChildren = false;
        using var folder3HasChildrenSub = folder3.Model.GetHasChildrenObservable_ForTestingOnly().Subscribe(x => folder3HasChildren = x);
        using var folder3Children = BindChildren(folder3.Model.GetChildrenObservable_ForTestingOnly(), out var folder3ChildrenCollection);
        folder3HasChildren.Should().BeTrue();
        folder3ChildrenCollection.Should().ContainSingle().Which.Should().Be(fileModel); // Contains file
        
        // Act - Remove the file
        generator.OnDeleteFile(filePath, fileModel);
        
        // Assert observables after deletion
        rootHasChildren.Should().BeFalse(); // No folders should be left, because inner folders became empty and started a delete chain
        rootChildrenCollection.Should().BeEmpty();
        folder1HasChildren.Should().BeFalse();
        folder1ChildrenCollection.Should().BeEmpty();
        folder2HasChildren.Should().BeFalse();
        folder2ChildrenCollection.Should().BeEmpty();
        folder3HasChildren.Should().BeFalse();
        folder3ChildrenCollection.Should().BeEmpty();
    }

    [Fact]
    public void OnDeleteFile_RemovesEmptyFolderChain_WhenFileDeletedFromNestedFolder()
    {
        // Arrange - Create a deeply nested folder structure with a single file
        var fileId = EntityId.From(0UL);
        var filePath = new RelativePath("parent1/parent2/parent3/file.txt");
        var fileModel = CreateFileModel(fileId);
        
        var generator = CreateGenerator();
        
        // Add the file to the nested folder structure
        generator.OnReceiveFile(filePath, fileModel);
        
        // Verify initial structure
        var rootFolder = generator.GetOrCreateFolder("", out _, out _);
        rootFolder.Folders.Count.Should().Be(1);
        
        var parent1 = rootFolder.Folders.Lookup("parent1").Value;
        parent1.Folders.Count.Should().Be(1);
        parent1.Files.Count.Should().Be(0);
        
        var parent2 = parent1.Folders.Lookup("parent2").Value;
        parent2.Folders.Count.Should().Be(1);
        parent2.Files.Count.Should().Be(0);
        
        var parent3 = parent2.Folders.Lookup("parent3").Value;
        parent3.Files.Count.Should().Be(1);
        parent3.Files.Lookup(fileId).HasValue.Should().BeTrue();
        
        // Act - Delete the file
        var result = generator.OnDeleteFile(filePath, fileModel);
        
        // Assert
        result.Should().BeTrue(); // The folder should be empty and deleted
        
        // Verify that all parent folders were deleted
        rootFolder = generator.GetOrCreateFolder("", out _, out _);
        rootFolder.Folders.Count.Should().Be(0); // parent1 should be deleted
        
        // Try to get the folders that should no longer exist
        var parent1Lookup = rootFolder.Folders.Lookup("parent1");
        parent1Lookup.HasValue.Should().BeFalse(); // parent1 should not exist
    }

    private static TreeFolderGeneratorForLocationId<TestTreeItemWithPath> CreateGenerator() => new();

    private static CompositeItemModel<EntityId> CreateFileModel(EntityId id) => new(id);
    
    private static IDisposable BindChildren(IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> childrenObservable, out ReadOnlyObservableCollection<CompositeItemModel<EntityId>> collection)
    {
        return childrenObservable
            .Bind(out collection)
            .Subscribe();
    }
}
