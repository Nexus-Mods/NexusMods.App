using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// A class responsible for creating 'folders' where we have views of files in a tree
/// using <see cref="CompositeItemModel{TKey}"/>. Each instance is tied with a <see cref="LocationId"/>
/// such as <see cref="LocationId.Game"/>, <see cref="LocationId.Saves"/> etc.
/// </summary>
/// <typeparam name="TTreeItemWithPath">The type used to denote the file in the tree.</typeparam>
/// <typeparam name="TFolderModelInitializer">The initializer for folder models.</typeparam>
public class TreeFolderGeneratorForLocationId<TTreeItemWithPath, TFolderModelInitializer> 
    where TTreeItemWithPath : ITreeItemWithPath
    where TFolderModelInitializer : IFolderModelInitializer<TTreeItemWithPath>
{
    /// <example>
    /// Represents all files at the 'root' of this <see cref="LocationId"/>
    /// </example>
    private readonly GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> _rootFolder = new();
    
    /// <summary>
    /// Obtains an observable set of <see cref="CompositeItemModel{TKey}"/> for the specified folder.
    /// </summary>
    public CompositeItemModel<EntityId> ModelForRoot()
    {
        return _rootFolder.Model;
    }
    
    /// <summary>
    /// Invoked on every file received from the caller.
    /// This adds the file to the inner tree.
    /// </summary>
    /// <param name="path">The relative path of the item (file) that was just read in.</param>
    /// <param name="itemModel">The <see cref="CompositeItemModel{TKey}"/> (tree node) for the file.</param>
    internal void OnReceiveFile(RelativePath path, CompositeItemModel<EntityId> itemModel)
    {
        var folderPath = path.Parent;
        var folder = GetOrCreateFolder(folderPath, out _, out _);
        folder.AddFileItemModel(itemModel);
    }
    
    /// <summary>
    /// Invoked on every file deleted from the caller.
    /// This removes the file from the inner tree.
    /// </summary>
    /// <param name="path">The relative path of the item (file) to be removed.</param>
    /// <param name="itemModel">The <see cref="CompositeItemModel{TKey}"/> (tree node) for the file.</param>
    /// <returns>True if the folder in which the path resides was deleted.</returns>
    internal bool OnDeleteFile(RelativePath path, CompositeItemModel<EntityId> itemModel)
    {
        var folder = GetOrCreateFolder(path.Parent, out var parentFolder, out var parentFolderName);
        var deleteFolder = folder.DeleteFileItemModel(itemModel.Key);

        if (deleteFolder)
        {
            parentFolder.DeleteSubfolder(parentFolderName);
            
            // Note(sewer): This is a very rare path in practice, deletes are very infrequent, so this
            //              path isn't necessarily super optimized.
            //              We should ideally be avoiding allocations there, and it is possible, but
            //              the paths library lacks what we need in Span'ified form.
            DeleteEmptyFolderChain(path.Parent);
        }

        return deleteFolder; 
    }
    
    /// <summary>
    /// Deletes empty folders along the path chain.
    /// </summary>
    /// <param name="folderPath">The path of the folder to check for emptiness.</param>
    private void DeleteEmptyFolderChain(RelativePath folderPath)
    {
        // Get all parent paths, from nearest to farthest (root)
        // Note: We use '.Parent' because `GetAllParents` includes the path itself.
        foreach (var parentPath in folderPath.Parent.GetAllParents())
        {
            var folder = GetOrCreateFolder(parentPath, out var parentFolder, out var parentFolderName);
            if (folder.ShouldDeleteFolder())
                parentFolder.DeleteSubfolder(parentFolderName);
            else
                break;
        }
    }

    /// <summary>
    /// Obtains the folder by navigating through the <see cref="_rootFolder"/> down
    /// to the specified folder. If the folder does not exist, it will be created.
    /// </summary>
    /// <param name="path">The path of the folder to obtain.</param>
    /// <param name="parentFolder">The parent of the folder returned.</param>
    /// <param name="parentFolderName">Name of the parent folder.</param>
    internal GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> GetOrCreateFolder(RelativePath path, out GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> parentFolder, out RelativePath parentFolderName)
    {
        // Go through all parents of the path, and create them if they don't exist.
        parentFolder = _rootFolder;
        parentFolderName = "";
        
        var currentFolder = _rootFolder;
        foreach (var part in path.GetParts())
        {
            parentFolder = currentFolder;
            currentFolder = currentFolder.GetOrCreateChildFolder(part);
            parentFolderName = part;
        }
        
        return currentFolder;
    }
}

/// <summary>
/// Represents a single folder generated by the <see cref="TreeFolderGeneratorForLocationId{TTreeItemWithPath, TFolderModelInitializer}"/>
/// </summary>
/// <remarks>
///     Note: Sewer. This should ideally be a struct, but due to limitations with DynamicData's API,
///     we're forced to make this a class.
/// </remarks>
public class GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> : IDisposable 
    where TTreeItemWithPath : ITreeItemWithPath
    where TFolderModelInitializer : IFolderModelInitializer<TTreeItemWithPath>
{
    /// <summary>
    /// The <see cref="CompositeItemModel{TKey}"/> representing the current folder node
    /// in the tree visually.
    /// </summary>
    /// <remarks>
    ///     We default to an invalid entity of type <see cref="EntityId.MaxValueNoPartition"/>, as this will need
    ///     to store children (files) which are MnemonicDB entities and thus require it (unlike folders).
    /// </remarks>
    public CompositeItemModel<EntityId> Model { get; }

    /// <summary>
    /// All of the <see cref="CompositeItemModel{TKey}"/>(s) for each file in this folder.
    /// </summary>
    /// <remarks>
    ///     The key is the unique entity ID for the file.
    ///     This powers the <see cref="TreeDataGridItemModel{TModel,TKey}.ChildrenObservable"/> field.
    /// </remarks>
    public SourceCache<CompositeItemModel<EntityId>, EntityId> Files = new(x => x.Key);
    
    /// <summary>
    /// All of the <see cref="CompositeItemModel{TKey}"/>(s) for each folder in this folder.
    /// </summary>
    /// <remarks>The key is the folder name, without any separators.</remarks>
    public SourceCache<GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer>, RelativePath> Folders = new(_ => null!);
    
    /// <summary>
    /// A source cache containing all files from this folder and all its subfolders recursively.
    /// </summary>
    /// <remarks>Used to power recursive file views.</remarks>
    private readonly SourceCache<CompositeItemModel<EntityId>, EntityId> _allFilesRecursive = new(x => x.Key);
    
    /// <summary>
    /// Subscription disposables for recursive file tracking.
    /// </summary>
    private readonly CompositeDisposable _subscriptions = new();

#if DEBUG
    // Flag indicating if this folder has been disposed. For test code only.
    public bool IsDisposed { get; private set; } = false;
#endif

    /// <summary/>
    public GeneratedFolder()
    {
        // Create observables for the children and hasChildren status.
        var filesCount = Files.CountChanged.Select(count => count > 0);
        var foldersCount = Folders.CountChanged.Select(count => count > 0);
        var hasChildrenObservable = Observable.CombineLatest(filesCount, foldersCount, (hasFiles, hasFolders) => hasFiles || hasFolders)
            .StartWith(false); // Start with false until counts are known

        var fileChildren = Files.Connect();
        // Transform the folder cache: connect, get the GeneratedFolder, then select its Model.
        // KeySelector is needed because the keys are different (EntityId for files, RelativePath for folders).
        // In the folders we use invalid EntityID(s) [EntityId.MaxValueNoPartition] to power the tree structure.
        var folderChildren = Folders.Connect()
            .Transform(folder => folder.Model)
            .ChangeKey(model => model.Key); // Ensure keys are EntityId

        var childrenObservable = fileChildren.Merge(folderChildren);

        // Initialize the Model property passing the created observables.
        Model = new CompositeItemModel<EntityId>(EntityId.MaxValueNoPartition)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable
        };
        
        // Initialize the model using the user provided function
        TFolderModelInitializer.InitializeModel(Model, this);
        
        // Set up recursive file tracking
        // 1. Add all direct files to the recursive collection
        _subscriptions.Add(Files.Connect().PopulateInto(_allFilesRecursive));
        
        // 2. Track subfolders and their files recursively
        _subscriptions.Add(
            Folders.Connect()
                .SubscribeMany(folder => 
                    folder.GetAllFilesRecursiveObservable()
                        .PopulateInto(_allFilesRecursive)
                )
                .Subscribe()
        );
    }

    /// <summary>
    /// Gets an observable of all files in this folder and all its subfolders recursively.
    /// </summary>
    /// <returns>An observable changeset of all file <see cref="CompositeItemModel{EntityId}"/> items.</returns>
    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetAllFilesRecursiveObservable() => _allFilesRecursive.Connect();

    /// <summary>
    /// Adds a file <see cref="CompositeItemModel{EntityId}"/> to this folder
    /// </summary>
    /// <param name="child">The child <see cref="CompositeItemModel{EntityId}"/></param>
    public void AddFileItemModel(CompositeItemModel<EntityId> child) => Files.AddOrUpdate(child);

    /// <summary>
    /// Removes a file CompositeItemModel from this folder
    /// </summary>
    /// <param name="key">The <see cref="EntityId"/> of the child to remove.</param>
    /// <returns>True if this folder is empty.</returns>
    public bool DeleteFileItemModel(EntityId key)
    {
        var alreadyPresent = Files.Lookup(key).HasValue;
        if (alreadyPresent)
            Files.Remove(key);

        return ShouldDeleteFolder();
    }

    /// <summary>
    /// Checks if this folder should be deleted.
    /// </summary>
    /// <returns>True if the folder is empty (no files) and has no subfolders.</returns>
    public bool ShouldDeleteFolder() => Files.Count == 0 && Folders.Count == 0;
    
    /// <summary>
    /// Gets or creates a child folder within this <see cref="GeneratedFolder{TTreeItemWithPath, TFolderModelInitializer}"/>
    /// </summary>
    /// <param name="folderName">
    ///     Name of the (single) folder, for example, 'saves'.
    ///     Should not contain separators or other subfolders.
    /// </param>
    public GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> GetOrCreateChildFolder(RelativePath folderName)
    {
        var optionalChild = Folders.Lookup(folderName);
        GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer>  result;
        if (optionalChild.HasValue)
            result = optionalChild.Value;
        else // create a new folder if not already exists
        {
            result = CreateChildFolder();
            // Note(sewer): No direct API without `Edit` that allows for manually specifying key.
            Folders.Edit(updater => updater.AddOrUpdate(result, folderName));
        }

        return result;
    }

    /// <summary>
    /// Deletes a subfolder with a given name.
    /// </summary>
    /// <param name="folderName">The name of the folder to delete from.</param>
    public void DeleteSubfolder(RelativePath folderName)
    {
        // Note(sewer): This sucks. In DynamicData you can't get value from the delete, neither
        // in the Edit API or the parent Remove API.
        var lookup = Folders.Lookup(folderName);
        if (lookup.HasValue)
        {
            var subfolder = lookup.Value;
            Folders.Remove(folderName);
            subfolder.Dispose();
        }
    }

    private static GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> CreateChildFolder() => new();

    public void Dispose()
    {
        Files.Dispose();
        Folders.Dispose();
        Model.Dispose();
        _allFilesRecursive.Dispose();
        _subscriptions.Dispose();
#if DEBUG
        IsDisposed = true;
#endif
    }
}
