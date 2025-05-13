using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
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
    private readonly GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> _rootFolder;
    
    /// <summary>
    /// Creates a folder generator for the given LocationId.
    /// </summary>
    /// <param name="rootFolderName">
    ///     Name of the 'root' node for this folder.
    ///     Does not need to necessarily constitute a real path, can be just 'GAME' etc,
    ///     just make sure locationId matches.
    /// </param>
    public TreeFolderGeneratorForLocationId(GamePath rootFolderName)
    {
        _rootFolder = new GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer>(rootFolderName);
    }
    
    /// <summary>
    /// Obtains an observable set of <see cref="CompositeItemModel{TKey}"/> for the specified folder.
    /// </summary>
    public CompositeItemModel<GamePath> ModelForRoot()
    {
        return _rootFolder.Model;
    }
    
    /// <summary>
    /// Returns an observable changeset of children items from the root folder.
    /// </summary>
    public IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> ObservableChildren()
    {
        return _rootFolder.Model.ChildrenObservable;
    }
    
    /// <summary>
    /// Invoked on every file received from the caller.
    /// This adds the file to the inner tree.
    /// </summary>
    /// <param name="path">The path of the item (file) that was just read in.</param>
    /// <param name="itemModel">The <see cref="CompositeItemModel{TKey}"/> (tree node) for the file.</param>
    internal void OnReceiveFile(GamePath path, CompositeItemModel<GamePath> itemModel)
    {
        var folderPath = path.Parent;
        var folder = GetOrCreateFolder(folderPath, out _, out _);
        folder.AddFileItemModel(itemModel);
    }
    
    /// <summary>
    /// Invoked on every file deleted from the caller.
    /// This removes the file from the inner tree.
    /// </summary>
    /// <param name="path">The path of the item (file) to be removed.</param>
    /// <param name="itemModel">The <see cref="CompositeItemModel{TKey}"/> (tree node) for the file.</param>
    /// <returns>True if the folder in which the path resides was deleted.</returns>
    internal bool OnDeleteFile(GamePath path, CompositeItemModel<GamePath> itemModel)
    {
        var folder = GetOrCreateFolder(path.Parent, out var parentFolder, out var parentFolderName);
        folder.DeleteFileItemModel(itemModel.Key);

        if (folder.ShouldDeleteFolder())
        {
            parentFolder.DeleteSubfolder(parentFolderName);
            
            // Note(sewer): This is a very rare path in practice, deletes are very infrequent, so this
            //              path isn't necessarily super optimized.
            //              We should ideally be avoiding allocations there, and it is possible, but
            //              the paths library lacks what we need in Span'ified form.
            return DeleteEmptyFolderChain(path.Parent);
        }

        return false; 
    }
    
    /// <summary>
    /// Deletes empty folders along the path chain.
    /// </summary>
    /// <param name="folderPath">The path of the folder to check for emptiness.</param>
    /// <returns>True if all the folders were deleted up to the top level, false otherwise</returns>
    private bool DeleteEmptyFolderChain(GamePath folderPath)
    {
        // Get all parent paths, from nearest to farthest (root)
        // Note: We use '.Parent' because `GetAllParents` includes the path itself.
        foreach (var parentPath in folderPath.Parent.GetAllParents())
        {
            var folder = GetOrCreateFolder(parentPath, out var parentFolder, out var parentFolderName);
            if (folder.ShouldDeleteFolder())
                parentFolder.DeleteSubfolder(parentFolderName);
            else
                return false;
        }

        return true;
    }

    /// <summary>
    /// Obtains the folder by navigating through the <see cref="_rootFolder"/> down
    /// to the specified folder. If the folder does not exist, it will be created.
    /// </summary>
    /// <param name="path">The path of the folder to obtain.</param>
    /// <param name="parentFolder">The parent of the folder returned.</param>
    /// <param name="parentFolderPath">Name of the parent folder.</param>
    internal GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> GetOrCreateFolder(GamePath path, out GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> parentFolder, out GamePath parentFolderPath)
    {
        // Go through all parents of the path, and create them if they don't exist.
        parentFolder = _rootFolder;
        parentFolderPath = GamePath.Empty(path.LocationId);
        
        var currentFolder = _rootFolder;
        foreach (var partAsGamePath in path.Path.GetAllParents().Reverse())
        {
            var part = partAsGamePath.FileName;
            parentFolder = currentFolder;
            parentFolderPath = new GamePath(path.LocationId, partAsGamePath);
            currentFolder = currentFolder.GetOrCreateChildFolder(part, parentFolderPath);
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
    public CompositeItemModel<GamePath> Model { get; }

    /// <summary>
    /// All of the <see cref="CompositeItemModel{TKey}"/>(s) for each file in this folder.
    /// </summary>
    /// <remarks>
    ///     The key is the full path to the file.
    ///     This powers the <see cref="TreeDataGridItemModel{TModel,TKey}.ChildrenObservable"/> field.
    /// </remarks>
    public readonly SourceCache<CompositeItemModel<GamePath>, GamePath> Files = new(x => x.Key);
    
    /// <summary>
    /// All of the <see cref="CompositeItemModel{TKey}"/>(s) for each folder in this folder.
    /// </summary>
    /// <remarks>
    ///     The key is the file name component of the subfolder.
    ///     For instance, if the path to the subfolder is 'game/data'
    ///     And this folder is 'game'.
    ///     Then the key is 'data'.
    /// </remarks>
    public readonly SourceCache<GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer>, GamePath> Folders = new(x => x.FullPath);

    /// <summary>
    /// Full path to the folder node.
    /// </summary>
    public GamePath FullPath { get; private set; }

    /// <summary>
    /// Name of this folder node.
    /// </summary>
    public RelativePath FolderName => FullPath.FileName;
    
    /// <summary>
    /// A source cache containing all files from this folder and all its subfolders recursively.
    /// </summary>
    /// <remarks>Used to power recursive file views.</remarks>
    private readonly SourceCache<CompositeItemModel<GamePath>, GamePath> _allFilesRecursive = new(x => x.Key);
    
    /// <summary>
    /// Subscription disposables for recursive file tracking.
    /// </summary>
    private readonly CompositeDisposable _subscriptions = new();

#if DEBUG
    // Flag indicating if this folder has been disposed. For test code only.
    public bool IsDisposed { get; private set; } = false;
#endif

    /// <summary/>
    /// <param name="fullPath">Full path on disk of this folder node.</param>
    public GeneratedFolder(GamePath fullPath)
    {
        FullPath = fullPath;
        
        // Create observables for the children and hasChildren status.
        var filesCount = Files.CountChanged.Select(count => count > 0);
        var foldersCount = Folders.CountChanged.Select(count => count > 0);
        var hasChildrenObservable = Observable.CombineLatest(filesCount, foldersCount, (hasFiles, hasFolders) => hasFiles || hasFolders)
            .StartWith(false); // Start with false until counts are known

        var fileChildren = Files.Connect();
        // Transform the folder cache: connect, get the GeneratedFolder, then select its Model.
        // So the type of both observables is the same.
        var folderChildren = Folders.Connect()
            .Transform(folder => folder.Model)
            .ChangeKey(x => x.Key); // Transform keys to GamePath by appending full folder path to sub-path component

        // Initialize the Model property passing the created observables.
        Model = new CompositeItemModel<GamePath>(fullPath)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = fileChildren.Merge(folderChildren),
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
    /// <returns>An observable changeset of all file <see cref="CompositeItemModel{GamePath}"/> items.</returns>
    public IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> GetAllFilesRecursiveObservable() => _allFilesRecursive.Connect();

    /// <summary>
    /// Adds a file <see cref="CompositeItemModel{GamePath}"/> to this folder
    /// </summary>
    /// <param name="child">The child <see cref="CompositeItemModel{GamePath}"/></param>
    public void AddFileItemModel(CompositeItemModel<GamePath> child) => Files.AddOrUpdate(child);

    /// <summary>
    /// Removes a file CompositeItemModel from this folder
    /// </summary>
    /// <param name="key">The full path of the item to remove.</param>
    /// <returns>True if the item was removed</returns>
    public bool DeleteFileItemModel(GamePath key)
    {
        if (!Files.Lookup(key).HasValue)
            return false;
        Files.Remove(key);
        return true;
    }

    /// <summary>
    /// Checks if this folder should be deleted.
    /// </summary>
    /// <returns>True if the folder is empty (no files) and has no subfolders.</returns>
    public bool ShouldDeleteFolder() => Files.Count == 0 && Folders.Count == 0;

    /// <summary>
    /// Gets or creates a child folder within this <see cref="GeneratedFolder{TTreeItemWithPath, TFolderModelInitializer}"/>
    /// </summary>
    /// <param name="part">
    ///     Name of the (single) folder, for example, 'saves'.
    ///     Should not contain separators or other subfolders.
    /// </param>
    /// <param name="fullPath">
    ///     Full path to the child folder.
    /// </param>
    public GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> GetOrCreateChildFolder(RelativePath part, GamePath fullPath)
    {
        var optionalChild = Folders.Lookup(fullPath);
        GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> result;
        if (optionalChild.HasValue)
            result = optionalChild.Value;
        else // create a new folder if not already exists
        {
            result = CreateChildFolder(fullPath);
            // Note(sewer): No direct API without `Edit` that allows for manually specifying key.
            Folders.Edit(updater => updater.AddOrUpdate(result, fullPath));
        }

        return result;
    }

    /// <summary>
    /// Deletes a subfolder with a given GamePath. Only for first level subfolders.
    /// </summary>
    /// <param name="folderPath">The gamePath of the subfolder to delete from.</param>
    public void DeleteSubfolder(GamePath folderPath)
    {
        // Note(sewer): This sucks. In DynamicData you can't get value from the delete, neither
        // in the Edit API or the parent Remove API.
        var lookup = Folders.Lookup(folderPath);
        if (lookup.HasValue)
        {
            var subfolder = lookup.Value;
            Folders.Remove(folderPath);
            subfolder.Dispose();
        }
    }

    private static GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> CreateChildFolder(GamePath folderPath) => new(folderPath);

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
