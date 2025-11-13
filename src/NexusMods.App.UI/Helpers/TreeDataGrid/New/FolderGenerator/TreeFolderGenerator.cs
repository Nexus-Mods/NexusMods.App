using DynamicData;
using NexusMods.App.UI.Controls;
using System.Reactive.Linq;
using NexusMods.Sdk.Games;

namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// A class responsible for creating 'folders' where we have views of files in a tree
/// using <see cref="CompositeItemModel{TKey}"/>.
/// </summary>
/// <typeparam name="TTreeItemWithPath">The type used to denote the file in the tree.</typeparam>
/// <typeparam name="TFolderModelInitializer">The initializer for folder models.</typeparam>
public class TreeFolderGenerator<TTreeItemWithPath, TFolderModelInitializer> : IDisposable
    where TTreeItemWithPath : ITreeItemWithPath
    where TFolderModelInitializer : IFolderModelInitializer<TTreeItemWithPath>
{
    internal readonly Dictionary<LocationId, TreeFolderGeneratorForLocationId<TTreeItemWithPath, TFolderModelInitializer>> LocationIdToTree = new();
    internal readonly SourceCache<CompositeItemModel<GamePath>, GamePath> RootCache = new(model => model.Key);
    private readonly IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> _observableRoots;

    public TreeFolderGenerator() => _observableRoots = RootCache.Connect().RefCount();

    /// <summary>
    /// Returns an observable changeset of root items, suitable for binding to a TreeDataGrid.
    /// </summary>
    public IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> ObservableRoots() => _observableRoots;

    /// <summary>
    /// A variant of <see cref="ObservableRoots"/> which returns the contents of
    /// a single 'LocationId' when there's only one root (LocationId). Used for
    /// better UI/UX experience.
    ///
    /// In simpler words, don't show the 'GAME' folder if we only have files in 'GAME'.
    /// But if we have 'GAME' and 'SAVES', show both!
    /// </summary>
    [Obsolete("Not usable yet, calling this causes a double dispose error when used from the UI. This needs investigation.")]
    public IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> SimplifiedObservableRoots()
    {
        return _observableRoots
            .Select(_ => LocationIdToTree.Count) // tied 1:1 with root count
            .Select(GetAdaptedChangeSet) // get either changeset with 1 root, or with all roots.
            .Switch();
    }

    private IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> GetAdaptedChangeSet(int count)
    {
        // Return all roots
        if (count != 1)
            return _observableRoots;

        // Else if there's only one location ID, return its children
        var singleRoot = LocationIdToTree.Values.First();
        return singleRoot.ObservableChildren();
    }

    /// <summary>
    /// Invoked on every file received from the caller.
    /// This adds the file to the inner tree.
    /// </summary>
    /// <param name="item">The item (file) that was just read in.</param>
    /// <param name="itemModel">The <see cref="CompositeItemModel{TKey}"/> (tree node) for the file.</param>
    public void OnReceiveFile(TTreeItemWithPath item, CompositeItemModel<GamePath> itemModel)
    {
        var path = item.GetPath();
        if (!LocationIdToTree.TryGetValue(path.LocationId, out var tree))
        {
            tree = new TreeFolderGeneratorForLocationId<TTreeItemWithPath, TFolderModelInitializer>(new GamePath(path.LocationId, string.Empty), path.LocationId.ToString());
            LocationIdToTree.Add(path.LocationId, tree);
            RootCache.AddOrUpdate(tree.ModelForRoot());
        }

        tree.OnReceiveFile(path, itemModel);
    }

    /// <summary>
    /// Invoked on every file deleted from the caller.
    /// This removes the file from the inner tree.
    /// </summary>
    /// <param name="item">The item to be removed.</param>
    /// <param name="itemModel">The <see cref="CompositeItemModel{TKey}"/> (tree node) for the file.</param>
    public void OnDeleteFile(TTreeItemWithPath item, CompositeItemModel<GamePath> itemModel)
    {
        var path = item.GetPath();
        if (!LocationIdToTree.TryGetValue(path.LocationId, out var tree))
            return;
        
        var rootBecameEmpty = tree.OnDeleteFile(path, itemModel);
        if (!rootBecameEmpty)
            return;
        
        // Root is empty, remove the model and location id.
        var rootModel = tree.ModelForRoot();
        RootCache.Remove(rootModel);
        LocationIdToTree.Remove(path.LocationId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        RootCache.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Adapter that processes changesets of tree items and passes them to a <see cref="TreeFolderGenerator{TTreeItemWithPath,TFolderModelInitializer}"/>.
/// For convenience, consider wrapping for concrete types like in <see cref="TreeFolderGeneratorLoadoutTreeItemAdapter{TFolderModelInitializer}"/>.
/// </summary>
/// <typeparam name="TKey">Type of key used for the changesets.</typeparam>
/// <typeparam name="TFolderModelInitializer">Type used for initializing folder models, like <see cref="DefaultFolderModelInitializer{TTreeItemWithPath}"/>.</typeparam>
/// <typeparam name="TTreeItemWithPath">Type of tree item that can provide a path. e.g. <see cref="GamePathTreeItemWithPath"/>.</typeparam>
/// <typeparam name="TTreeItemWithPathFactory">A factory that creates <typeparamref name="TTreeItemWithPath"/></typeparam>
/// <remarks>
///     Subscription is dropped when this item is GC'd.
///     For an integration example, look at "ViewLoadoutGroupFilesTreeDataGridAdapter". 
/// </remarks>
public class TreeFolderGeneratorCompositeItemModelAdapter<TTreeItemWithPath, TTreeItemWithPathFactory, TKey, TFolderModelInitializer> : IDisposable
    where TTreeItemWithPath : ITreeItemWithPath
    where TTreeItemWithPathFactory : ITreeItemWithPathFactory<GamePath, TTreeItemWithPath>
    where TKey : notnull
    where TFolderModelInitializer : IFolderModelInitializer<TTreeItemWithPath>
{
    /// <summary>
    /// The under the hood generated 'folder generator' for this adapter.
    /// </summary>
    public readonly TreeFolderGenerator<TTreeItemWithPath, TFolderModelInitializer> FolderGenerator;
    private readonly TTreeItemWithPathFactory _factory;
    private readonly IDisposable _adaptDisposable;

    /// <summary>
    /// Creates a new instance of <see cref="TreeFolderGeneratorCompositeItemModelAdapter{TTreeItemWithPath,TTreeItemWithPathFactory,TKey,TFolderModelInitializer}"/>.
    /// </summary>
    /// <param name="factory">Factory that creates <typeparamref name="TTreeItemWithPath"/> instances.</param>
    /// <param name="changes">Observable of changes to automatically process.</param>
    public TreeFolderGeneratorCompositeItemModelAdapter(
        TTreeItemWithPathFactory factory,
        IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> changes)
    {
        _factory = factory;
        FolderGenerator = new TreeFolderGenerator<TTreeItemWithPath, TFolderModelInitializer>();
        
        // Subscribe to the changes and pipe them into Adapt
        _adaptDisposable = changes.Subscribe(Adapt);
    }

    /// <summary>
    /// Adapts changes from a changeset by calling the appropriate methods on the folder generator.
    /// </summary>
    /// <param name="changes">The changeset containing changes to process.</param>
    private void Adapt(IChangeSet<CompositeItemModel<GamePath>, GamePath> changes)
    {
        foreach (var change in changes)
        {
            switch (change.Reason)
            {
                case ChangeReason.Add:
                case ChangeReason.Update:
                    FolderGenerator.OnReceiveFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                case ChangeReason.Remove:
                    FolderGenerator.OnDeleteFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                case ChangeReason.Refresh:
                    // Refresh can be treated as an update
                    FolderGenerator.OnReceiveFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                case ChangeReason.Moved:
                    // Note(sewer): This case is not tested, I don't know how to trigger
                    // this event.
                    FolderGenerator.OnDeleteFile(_factory.CreateItem(change.Current.Key), change.Current);
                    FolderGenerator.OnReceiveFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change.Reason), change.Reason, @"Unhandled change reason");
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _adaptDisposable.Dispose();
        FolderGenerator.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// A <see cref="TreeFolderGeneratorCompositeItemModelAdapter{TTreeItemWithPath,TTreeItemWithPathFactory,TKey,TFolderModelInitializer}"/>
/// for storing LoadoutItem(s) (<see cref="GamePathTreeItemWithPath"/>) 
/// </summary>
/// <typeparam name="TFolderModelInitializer"></typeparam>
public class TreeFolderGeneratorLoadoutTreeItemAdapter<TFolderModelInitializer> : TreeFolderGeneratorCompositeItemModelAdapter
<
    GamePathTreeItemWithPath, // Item
    GamePathTreeItemWithPathFactory, // Factory (supplied by this type)
    GamePath, // We make LoadoutItemTreeItemWithPath from EntityId
    TFolderModelInitializer // Column info.
> where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
{
    /// <inheritdoc />
    public TreeFolderGeneratorLoadoutTreeItemAdapter(IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> changes) : base(new GamePathTreeItemWithPathFactory(), changes) { }
}
