using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using System.Reactive.Linq;

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
    internal readonly SourceCache<CompositeItemModel<EntityId>, EntityId> RootCache = new(model => model.Key);
    private readonly IncrementingNumberGenerator _incrementingNumberGenerator = new();
    private readonly IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> _observableRoots;

    public TreeFolderGenerator() => _observableRoots = RootCache.Connect().RefCount();

    /// <summary>
    /// Returns an observable changeset of root items, suitable for binding to a TreeDataGrid.
    /// </summary>
    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObservableRoots() => _observableRoots;

    /// <summary>
    /// A variant of <see cref="ObservableRoots"/> which returns the contents of
    /// a single 'LocationId' when there's only one root (LocationId). Used for
    /// better UI/UX experience.
    ///
    /// In simpler words, don't show the 'GAME' folder if we only have files in 'GAME'.
    /// But if we have 'GAME' and 'SAVES', show both!
    /// </summary>
    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> SimplifiedObservableRoots()
    {
        return _observableRoots
            .Select(_ => LocationIdToTree.Count) // tied 1:1 with root count
            .Select(GetAdaptedChangeSet) // get either changeset with 1 root, or with all roots.
            .Switch();
    }

    private IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetAdaptedChangeSet(int count)
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
    public void OnReceiveFile(TTreeItemWithPath item, CompositeItemModel<EntityId> itemModel)
    {
        var path = item.GetPath();
        if (!LocationIdToTree.TryGetValue(path.LocationId, out var tree))
        {
            tree = new TreeFolderGeneratorForLocationId<TTreeItemWithPath, TFolderModelInitializer>(path.LocationId.ToString(), _incrementingNumberGenerator);
            LocationIdToTree.Add(path.LocationId, tree);
            RootCache.AddOrUpdate(tree.ModelForRoot());
        }

        tree.OnReceiveFile(path.Path, itemModel);
    }

    /// <summary>
    /// Invoked on every file deleted from the caller.
    /// This removes the file from the inner tree.
    /// </summary>
    /// <param name="item">The item to be removed.</param>
    /// <param name="itemModel">The <see cref="CompositeItemModel{TKey}"/> (tree node) for the file.</param>
    public void OnDeleteFile(TTreeItemWithPath item, CompositeItemModel<EntityId> itemModel)
    {
        var path = item.GetPath();
        if (!LocationIdToTree.TryGetValue(path.LocationId, out var tree))
            return;
        
        var rootBecameEmpty = tree.OnDeleteFile(path.Path, itemModel);
        if (!rootBecameEmpty)
            return;
        
        // Root is empty, remove the model and location id.
        var rootModel = tree.ModelForRoot();
        RootCache.Remove(rootModel);
        LocationIdToTree.Remove(path.LocationId);
        rootModel.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        RootCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
