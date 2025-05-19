#pragma warning disable CS0618 // Type or member is obsolete. Uses internal APIs for testing only.
using FluentAssertions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.Abstractions.GameLocators;
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
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "file.txt");
        var fileModel = CreateFileModel(filePath);

        var generator = CreateGenerator();

        // Act
        generator.OnReceiveFile(filePath, fileModel);

        // Assert
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        rootFolder.Files.Lookup(filePath).HasValue.Should().BeTrue();
        rootFolder.Files.Lookup(filePath).Value.Should().Be(fileModel);
        rootFolder.Files.Count.Should().Be(1);
        rootFolder.Folders.Count.Should().Be(0);
    }

    [Fact]
    public void OnReceiveFile_AddsFileToNestedFolder_WhenPathHasParents()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "folder1/folder2/file.txt");
        var fileModel = CreateFileModel(filePath);
        
        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        rootFolder.Files.Count.Should().Be(0);
        rootFolder.Folders.Count.Should().Be(1);

        // Navigate to folder1
        var folder1 = rootFolder.Folders.Lookup(new GamePath(locationId, "folder1")).Value;
        folder1.Files.Count.Should().Be(0);
        folder1.Folders.Count.Should().Be(1);
        folder1.FolderName.Should().Be((RelativePath)"folder1");
        
        // Navigate to folder2
        var folder2 = folder1.Folders.Lookup(new GamePath(locationId, "folder1/folder2")).Value;
        folder2.Files.Count.Should().Be(1);
        folder2.Files.Lookup(filePath).HasValue.Should().BeTrue();
        folder2.Files.Lookup(filePath).Value.Should().Be(fileModel);
        folder2.FolderName.Should().Be((RelativePath)"folder2");
    }
    
    [Fact]
    public void OnReceiveFile_IncrementsRefCount_OnlyForNewFiles()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "folder/file.txt");
        var fileModel = CreateFileModel(filePath);
        
        // Act
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath, fileModel);
        
        // Get folder and check initial refcount
        var folder = generator.GetOrCreateFolder(new GamePath(locationId, "folder"), out _);
        folder.Files.Count.Should().Be(1);

        // Act again with same file - should not increment refcount
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert
        folder.Files.Count.Should().Be(1);
        
        // Act again with different file - should increment refcount
        var fileId2 = new GamePath(locationId, "folder/file2.txt");
        var fileModel2 = CreateFileModel(fileId2);
        generator.OnReceiveFile(fileId2, fileModel2);
        
        // Assert
        folder.Files.Count.Should().Be(2);
    }
    
    [Fact]
    public void OnDeleteFile_RemovesFile_AndReturnsTrueWhenFolderEmpty()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "folder/file.txt");
        var fileModel = CreateFileModel(filePath);
        
        // Setup
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath, fileModel);

        // Act
        var result = generator.OnDeleteFile(filePath, fileModel);
        
        // Assert
        result.Should().BeTrue(); // Folder should be empty now
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        rootFolder.Folders.Count.Should().Be(0); // Folder should be removed
    }
    
    [Fact]
    public void OnDeleteFile_RemovesFile_AndReturnsFalseWhenFolderNotEmpty()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath1 = new GamePath(locationId, "folder/file1.txt");
        var filePath2 = new GamePath(locationId, "folder/file2.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var fileModel2 = CreateFileModel(filePath2);
        
        // Setup
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath1, fileModel1);
        generator.OnReceiveFile(filePath2, fileModel2);
        
        // Act
        var result = generator.OnDeleteFile(filePath1, fileModel1);
        
        // Assert
        result.Should().BeFalse(); // Folder should not be empty
        var folder = generator.GetOrCreateFolder(new GamePath(locationId, "folder"), out _);
        folder.Files.Count.Should().Be(1);
        folder.Files.Lookup(filePath1).HasValue.Should().BeFalse();
        folder.Files.Lookup(filePath2).HasValue.Should().BeTrue();
    }
    
    [Fact]
    public void GetOrCreateFolder_CreatesNestedFolders_WhenTheyDontExist()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var path = new GamePath(locationId, "folder1/folder2/folder3");
        
        // Act
        var generator = CreateGenerator();
        var folder = generator.GetOrCreateFolder(path, out var parentFolder);
        
        // Assert
        folder.Should().NotBeNull();
        parentFolder.Should().NotBeNull();
        folder.FullPath.Should().Be(path);
        folder.FolderName.Should().Be((RelativePath)"folder3");

        // Navigate from root to confirm structure
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        rootFolder.Folders.Count.Should().Be(1);
        
        var folder1 = rootFolder.Folders.Lookup(new GamePath(locationId, "folder1")).Value;
        folder1.Folders.Count.Should().Be(1);
        folder1.FolderName.Should().Be((RelativePath)"folder1");
        
        var folder2 = folder1.Folders.Lookup(new GamePath(locationId, "folder1/folder2")).Value;
        folder2.Folders.Count.Should().Be(1);
        folder2.FolderName.Should().Be((RelativePath)"folder2");
        
        var folder3 = folder2.Folders.Lookup(new GamePath(locationId, "folder1/folder2/folder3")).Value;
        folder3.Should().BeSameAs(folder);
        folder3.FolderName.Should().Be((RelativePath)"folder3");
    }
    
    [Fact]
    public void GetOrCreateFolder_ReturnsExistingFolder_WhenItExists()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var path = new GamePath(locationId, "folder1/folder2");
        
        // Act - create folder first time
        var generator = CreateGenerator();
        var initialFolder = generator.GetOrCreateFolder(path, out _);
        
        // Act - get the same folder again
        var retrievedFolder = generator.GetOrCreateFolder(path, out var parentFolder);
        
        // Assert
        retrievedFolder.Should().BeSameAs(initialFolder);
        parentFolder.Should().NotBeNull();
        retrievedFolder.FullPath.Should().Be(new GamePath(locationId, "folder1/folder2"));
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
        model.Should().BeSameAs(generator.GetOrCreateFolder(new GamePath(LocationId.From(1), ""), out _).Model);
    }
    
    // Observable-specific tests

    [Fact]
    public void RootObservables_InitializeCorrectly_WhenEmpty()
    {
        // Arrange
        var generator = CreateGenerator();
        var hasChildren = false;
        
        // Act
        using var hasChildrenSub = generator.ModelForRoot().HasChildrenObservable.Subscribe(x => hasChildren = x);
        using var children = BindChildren(generator.ModelForRoot().ChildrenObservable, out var childrenCollection);
        
        // Assert
        hasChildren.Should().BeFalse();
        childrenCollection.Should().BeEmpty();
    }
    
    [Fact]
    public void RootObservables_UpdateCorrectly_WhenFileAddedToRoot()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "file.txt");
        var fileModel = CreateFileModel(filePath);
        var generator = CreateGenerator();
        var hasChildren = false;
        
        using var hasChildrenSub = generator.ModelForRoot().HasChildrenObservable.Subscribe(x => hasChildren = x);
        using var children = BindChildren(generator.ModelForRoot().ChildrenObservable, out var childrenCollection);
        
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
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "folder/file.txt");
        var fileModel = CreateFileModel(filePath);
        var generator = CreateGenerator();
        var rootHasChildren = false;
        
        using var rootHasChildrenSub = generator.ModelForRoot().HasChildrenObservable.Subscribe(x => rootHasChildren = x);
        using var rootChildren = BindChildren(generator.ModelForRoot().ChildrenObservable, out var rootChildrenCollection);
        
        // Assert initial state
        rootHasChildren.Should().BeFalse();
        rootChildrenCollection.Should().BeEmpty();
        
        // Act
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert root observables
        rootHasChildren.Should().BeTrue();
        rootChildrenCollection.Should().ContainSingle(); // Should contain the folder
        
        // Get folder and check its observables
        var folder = generator.GetOrCreateFolder(new GamePath(locationId, "folder"), out _);
        var folderHasChildren = false;
        using var folderHasChildrenSub = folder.Model.HasChildrenObservable.Subscribe(x => folderHasChildren = x);
        using var folderChildren = BindChildren(folder.Model.ChildrenObservable, out var folderChildrenCollection);
        
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
        var locationId = LocationId.From(1);
        var filePath1 = new GamePath(locationId, "folder/file1.txt");
        var filePath2 = new GamePath(locationId, "folder/file2.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var fileModel2 = CreateFileModel(filePath2);
        var generator = CreateGenerator();
        var rootHasChildren = false;
        
        using var rootHasChildrenSub = generator.ModelForRoot().HasChildrenObservable.Subscribe(x => rootHasChildren = x);
        using var rootChildren = BindChildren(generator.ModelForRoot().ChildrenObservable, out var rootChildrenCollection);
        
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
        var folder = generator.GetOrCreateFolder(new GamePath(locationId, "folder"), out _);
        var folderHasChildren = false;
        using var folderHasChildrenSub = folder.Model.HasChildrenObservable.Subscribe(x => folderHasChildren = x);
        using var folderChildren = BindChildren(folder.Model.ChildrenObservable, out var folderChildrenCollection);
        
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
        rootHasChildren.Should().BeFalse(); // Root has no more folders
        rootChildrenCollection.Should().BeEmpty(); // No folders in root
    }
    
    [Fact]
    public void NestedFolderObservables_UpdateCorrectly_WhenFileAddedAndRemoved()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "folder1/folder2/folder3/file.txt");
        var fileModel = CreateFileModel(filePath);
        var generator = CreateGenerator();
        var rootHasChildren = false;
        
        using var rootHasChildrenSub = generator.ModelForRoot().HasChildrenObservable.Subscribe(x => rootHasChildren = x);
        using var rootChildren = BindChildren(generator.ModelForRoot().ChildrenObservable, out var rootChildrenCollection);
        
        // Assert initial state
        rootHasChildren.Should().BeFalse();
        rootChildrenCollection.Should().BeEmpty();
        
        // Act - Add file to deeply nested folder
        generator.OnReceiveFile(filePath, fileModel);
        
        // Assert root observables
        rootHasChildren.Should().BeTrue();
        rootChildrenCollection.Should().ContainSingle(); // Contains folder1
        
        // Get all folders
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        var folder1 = rootFolder.Folders.Lookup(new GamePath(locationId, "folder1")).Value;
        var folder2 = folder1.Folders.Lookup(new GamePath(locationId, "folder1/folder2")).Value;
        var folder3 = folder2.Folders.Lookup(new GamePath(locationId, "folder1/folder2/folder3")).Value;
        
        // Check folder1 observables
        var folder1HasChildren = false;
        using var folder1HasChildrenSub = folder1.Model.HasChildrenObservable.Subscribe(x => folder1HasChildren = x);
        using var folder1Children = BindChildren(folder1.Model.ChildrenObservable, out var folder1ChildrenCollection);
        folder1HasChildren.Should().BeTrue();
        folder1ChildrenCollection.Should().ContainSingle(); // Contains folder2
        folder1.FolderName.Should().Be((RelativePath)"folder1");
        
        // Check folder2 observables
        var folder2HasChildren = false;
        using var folder2HasChildrenSub = folder2.Model.HasChildrenObservable.Subscribe(x => folder2HasChildren = x);
        using var folder2Children = BindChildren(folder2.Model.ChildrenObservable, out var folder2ChildrenCollection);
        folder2HasChildren.Should().BeTrue();
        folder2ChildrenCollection.Should().ContainSingle(); // Contains folder3
        folder2.FolderName.Should().Be((RelativePath)"folder2");
        
        // Check folder3 observables
        var folder3HasChildren = false;
        using var folder3HasChildrenSub = folder3.Model.HasChildrenObservable.Subscribe(x => folder3HasChildren = x);
        using var folder3Children = BindChildren(folder3.Model.ChildrenObservable, out var folder3ChildrenCollection);
        folder3HasChildren.Should().BeTrue();
        folder3ChildrenCollection.Should().ContainSingle().Which.Should().Be(fileModel); // Contains file
        folder3.FolderName.Should().Be((RelativePath)"folder3");
        
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
        var locationId = LocationId.From(1);
        var filePath = new GamePath(locationId, "parent1/parent2/parent3/file.txt");
        var fileModel = CreateFileModel(filePath);
        
        var generator = CreateGenerator();
        
        // Add the file to the nested folder structure
        generator.OnReceiveFile(filePath, fileModel);
        
        // Verify initial structure
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        rootFolder.Folders.Count.Should().Be(1);
        
        var parent1 = rootFolder.Folders.Lookup(new GamePath(locationId, "parent1")).Value;
        parent1.Folders.Count.Should().Be(1);
        parent1.Files.Count.Should().Be(0);
        parent1.FolderName.Should().Be((RelativePath)"parent1");
        
        var parent2 = parent1.Folders.Lookup(new GamePath(locationId, "parent1/parent2")).Value;
        parent2.Folders.Count.Should().Be(1);
        parent2.Files.Count.Should().Be(0);
        parent2.FolderName.Should().Be((RelativePath)"parent2");
        
        var parent3 = parent2.Folders.Lookup(new GamePath(locationId, "parent1/parent2/parent3")).Value;
        parent3.Files.Count.Should().Be(1);
        parent3.Files.Lookup(filePath).HasValue.Should().BeTrue();
        parent3.FolderName.Should().Be((RelativePath)"parent3");
        
        // Act - Delete the file
        var result = generator.OnDeleteFile(filePath, fileModel);
        
        // Assert
        result.Should().BeTrue(); // The folder should be empty and deleted
        
        // Verify that all parent folders were deleted
        rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        rootFolder.Folders.Count.Should().Be(0); // parent1 should be deleted
        
        // Try to get the folders that should no longer exist
        var parent1Lookup = rootFolder.Folders.Lookup(new GamePath(locationId, "parent1"));
        parent1Lookup.HasValue.Should().BeFalse(); // parent1 should not exist
    }

    [Fact]
    public void GetAllFilesRecursiveObservable_ReturnsEmpty_WhenFolderHasNoFiles()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var generator = CreateGenerator();
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        
        // Act
        using var allFilesSubscription = rootFolder.GetAllFilesRecursiveObservable()
            .Bind(out var filesCollection)
            .Subscribe();
        
        // Assert
        filesCollection.Should().BeEmpty();
    }
    
    [Fact]
    public void GetAllFilesRecursiveObservable_ReturnsDirectFiles_WhenFolderHasOnlyDirectFiles()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath1 = new GamePath(locationId, "file1.txt");
        var filePath2 = new GamePath(locationId, "file2.txt");
        var fileModel1 = CreateFileModel(filePath1);
        var fileModel2 = CreateFileModel(filePath2);
        
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath1, fileModel1);
        generator.OnReceiveFile(filePath2, fileModel2);
        
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        
        // Act
        using var allFilesSubscription = rootFolder.GetAllFilesRecursiveObservable()
            .Bind(out var filesCollection)
            .Subscribe();
        
        // Assert
        filesCollection.Should().HaveCount(2);
        filesCollection.Should().Contain(fileModel1);
        filesCollection.Should().Contain(fileModel2);
    }
    
    [Fact]
    public void GetAllFilesRecursiveObservable_ReturnsAllFiles_WhenFilesAreInSubfolders()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath1 = new GamePath(locationId, "file1.txt");                // Root file
        var filePath2 = new GamePath(locationId, "folder1/file2.txt");        // In subfolder
        var filePath3 = new GamePath(locationId, "folder1/folder2/file3.txt"); // In nested subfolder
        
        var fileModel1 = CreateFileModel(filePath1);
        var fileModel2 = CreateFileModel(filePath2);
        var fileModel3 = CreateFileModel(filePath3);
        
        var generator = CreateGenerator();
        generator.OnReceiveFile(filePath1, fileModel1);
        generator.OnReceiveFile(filePath2, fileModel2);
        generator.OnReceiveFile(filePath3, fileModel3);
        
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        
        // Act - Get recursive files from root
        using var rootFilesSubscription = rootFolder.GetAllFilesRecursiveObservable()
            .Bind(out var rootFilesCollection)
            .Subscribe();
        
        // Assert - Root should see all files
        rootFilesCollection.Should().HaveCount(3);
        rootFilesCollection.Should().Contain(fileModel1);
        rootFilesCollection.Should().Contain(fileModel2);
        rootFilesCollection.Should().Contain(fileModel3);
        
        // Act - Get recursive files from folder1
        var folder1 = rootFolder.Folders.Lookup(new GamePath(locationId, "folder1")).Value;
        using var folder1FilesSubscription = folder1.GetAllFilesRecursiveObservable()
            .Bind(out var folder1FilesCollection)
            .Subscribe();
        
        // Assert - folder1 should see its file and the one in its subfolder
        folder1FilesCollection.Should().HaveCount(2);
        folder1FilesCollection.Should().Contain(fileModel2);
        folder1FilesCollection.Should().Contain(fileModel3);
        
        // Act - Get recursive files from folder2
        var folder2 = folder1.Folders.Lookup(new GamePath(locationId, "folder1/folder2")).Value;
        using var folder2FilesSubscription = folder2.GetAllFilesRecursiveObservable()
            .Bind(out var folder2FilesCollection)
            .Subscribe();
        
        // Assert - folder2 should see only its file
        folder2FilesCollection.Should().HaveCount(1);
        folder2FilesCollection.Should().Contain(fileModel3);
    }
    
    [Fact]
    public void GetAllFilesRecursiveObservable_UpdatesCorrectly_WhenFilesAreAddedOrRemoved()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath1 = new GamePath(locationId, "file1.txt");
        var filePath2 = new GamePath(locationId, "folder1/file2.txt");
        var filePath3 = new GamePath(locationId, "folder1/folder2/file3.txt");
        
        var fileModel1 = CreateFileModel(filePath1);
        var fileModel2 = CreateFileModel(filePath2);
        var fileModel3 = CreateFileModel(filePath3);
        
        var generator = CreateGenerator();
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        
        // Set up subscription to monitor changes
        using var rootFilesSubscription = rootFolder.GetAllFilesRecursiveObservable()
            .Bind(out var rootFilesCollection)
            .Subscribe();
        
        // Assert initial state
        rootFilesCollection.Should().BeEmpty();
        
        // Act 1 - Add a file to root
        generator.OnReceiveFile(filePath1, fileModel1);
        
        // Assert 1
        rootFilesCollection.Should().HaveCount(1);
        rootFilesCollection.Should().Contain(fileModel1);
        
        // Act 2 - Add a file to subfolder
        generator.OnReceiveFile(filePath2, fileModel2);
        
        // Assert 2 - Root should see the file
        rootFilesCollection.Should().HaveCount(2);
        rootFilesCollection.Should().Contain(fileModel1);
        rootFilesCollection.Should().Contain(fileModel2);
        
        // Act 3 - Add a file to nested subfolder
        generator.OnReceiveFile(filePath3, fileModel3);
        
        // Assert 3 - Root should see the file
        rootFilesCollection.Should().HaveCount(3);
        rootFilesCollection.Should().Contain(fileModel1);
        rootFilesCollection.Should().Contain(fileModel2);
        rootFilesCollection.Should().Contain(fileModel3);
        
        // Act 4 - Remove file from nested subfolder
        generator.OnDeleteFile(filePath3, fileModel3);
        
        // Assert 4 - Root should no longer see the file
        rootFilesCollection.Should().HaveCount(2);
        rootFilesCollection.Should().Contain(fileModel1);
        rootFilesCollection.Should().Contain(fileModel2);
        rootFilesCollection.Should().NotContain(fileModel3);
        
        // Act 5 - Remove file from subfolder
        generator.OnDeleteFile(filePath2, fileModel2);
        
        // Assert 5 - Root should no longer see the file
        rootFilesCollection.Should().HaveCount(1);
        rootFilesCollection.Should().Contain(fileModel1);
        rootFilesCollection.Should().NotContain(fileModel2);
        
        // Act 6 - Remove file from root
        generator.OnDeleteFile(filePath1, fileModel1);
        
        // Assert 6 - No files
        rootFilesCollection.Should().BeEmpty();
    }
    
    [Fact]
    public void GetAllFilesRecursiveObservable_HandlesNewlyAddedSubfoldersCorrectly()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var filePath1 = new GamePath(locationId, "file1.txt");
        var filePath2 = new GamePath(locationId, "folder1/file2.txt");
        var filePath3 = new GamePath(locationId, "folder1/folder2/file3.txt");
        
        var fileModel1 = CreateFileModel(filePath1);
        var fileModel2 = CreateFileModel(filePath2);
        var fileModel3 = CreateFileModel(filePath3);
        
        var generator = CreateGenerator();
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        
        // Set up subscription to monitor changes
        using var rootFilesSubscription = rootFolder.GetAllFilesRecursiveObservable()
            .Bind(out var rootFilesCollection)
            .Subscribe();
        
        // Act 1 - Add file to root
        generator.OnReceiveFile(filePath1, fileModel1);
        
        // Assert 1
        rootFilesCollection.Should().HaveCount(1);
        rootFilesCollection.Should().Contain(fileModel1);
        
        // Act 2 - Add file to a subfolder that doesn't exist yet (will be created)
        generator.OnReceiveFile(filePath2, fileModel2);
        
        // Assert 2
        rootFilesCollection.Should().HaveCount(2);
        rootFilesCollection.Should().Contain(fileModel1);
        rootFilesCollection.Should().Contain(fileModel2);
        
        // Act 3 - Add file to a nested subfolder that doesn't exist yet (will be created)
        generator.OnReceiveFile(filePath3, fileModel3);
        
        // Assert 3
        rootFilesCollection.Should().HaveCount(3);
        rootFilesCollection.Should().Contain(fileModel1);
        rootFilesCollection.Should().Contain(fileModel2);
        rootFilesCollection.Should().Contain(fileModel3);
    }

#if DEBUG
    [Fact]
    public void DeleteSubfolder_DisposesTheFolder_WhenRemoved_DebugOnly()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var generator = CreateGenerator();
        
        // Create a nested folder structure
        var filePath = new GamePath(locationId, "parent1/parent2/file.txt");
        var fileModel = CreateFileModel(filePath);
        
        generator.OnReceiveFile(filePath, fileModel);
        
        // Get the root folder and verify the structure
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        rootFolder.Folders.Count.Should().Be(1);
        
        // Get parent1 folder
        var parent1 = rootFolder.Folders.Lookup(new GamePath(locationId, "parent1")).Value;
        parent1.Folders.Count.Should().Be(1);
        
        // Get parent2 folder
        var parent2 = parent1.Folders.Lookup(new GamePath(locationId, "parent1/parent2")).Value;
        parent2.Files.Count.Should().Be(1);
        
        // Act - Delete the parent2 folder
        parent1.DeleteSubfolder(new GamePath(locationId, "parent1/parent2"));
        
        // Assert
        parent1.Folders.Count.Should().Be(0);
        parent2.IsDisposed.Should().BeTrue("The subfolder should be disposed when deleted");
    }

    [Fact]
    public void DeleteEmptyFolderChain_DisposesAllFolders_WhenRemoved_DebugOnly()
    {
        // Arrange
        var locationId = LocationId.From(1);
        var generator = CreateGenerator();
        
        // Create a nested folder structure
        var filePath = new GamePath(locationId, "parent1/parent2/parent3/file.txt");
        var fileModel = CreateFileModel(filePath);
        
        generator.OnReceiveFile(filePath, fileModel);
        
        // Get the root folder and verify the structure
        var rootFolder = generator.GetOrCreateFolder(new GamePath(locationId, ""), out _);
        var parent1 = rootFolder.Folders.Lookup(new GamePath(locationId, "parent1")).Value;
        var parent2 = parent1.Folders.Lookup(new GamePath(locationId, "parent1/parent2")).Value;
        var parent3 = parent2.Folders.Lookup(new GamePath(locationId, "parent1/parent2/parent3")).Value;
        
        // Store references for later assertions
        var parent1Ref = parent1;
        var parent2Ref = parent2;
        var parent3Ref = parent3;
        
        // Act - Delete the file and check if empty folders are removed
        var result = generator.OnDeleteFile(filePath, fileModel);
        
        // Assert
        result.Should().BeTrue("All folders should be empty and removed");
        rootFolder.Folders.Count.Should().Be(0);
        
        parent1Ref.IsDisposed.Should().BeTrue("parent1 should be disposed when empty");
        parent2Ref.IsDisposed.Should().BeTrue("parent2 should be disposed when empty");
        parent3Ref.IsDisposed.Should().BeTrue("parent3 should be disposed when empty");
    }
#endif

    private TreeFolderGeneratorForLocationId<GamePathTreeItemWithPath, DefaultFolderModelInitializer<GamePathTreeItemWithPath>> CreateGenerator() => 
        new(GamePath.Empty(LocationId.From(1)));

    private static CompositeItemModel<GamePath> CreateFileModel(GamePath gamePath) => new(gamePath);

    private static IDisposable BindChildren(IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> childrenObservable, out ReadOnlyObservableCollection<CompositeItemModel<GamePath>> collection)
    {
        return childrenObservable
            .Bind(out collection)
            .Subscribe();
    }
}
