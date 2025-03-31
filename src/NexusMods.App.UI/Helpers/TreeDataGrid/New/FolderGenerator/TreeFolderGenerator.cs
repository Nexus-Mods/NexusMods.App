using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// A class responsible for creating 'folders' where we have views of files in a tree
/// using <see cref="CompositeItemModel{TKey}"/>.
/// </summary>
/// <typeparamref name="TTreeItemWithPath">The type used to denote the file in the tree.</typeparamref>
public class TreeFolderGenerator<TTreeItemWithPath> where TTreeItemWithPath : ITreeItemWithPath
{
    private readonly Dictionary<LocationId, TreeFolderGeneratorForLocationId<TTreeItemWithPath>> _locationIdToTree = new();
    private readonly SourceCache<CompositeItemModel<EntityId>, EntityId> _rootCache = new(model => model.Key); // Assuming CompositeItemModel<EntityId> has EntityId Key

    /// <summary>
    /// Returns an observable changeset of root items, suitable for binding to a TreeDataGrid.
    /// </summary>
    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObservableRoots()
    {
        return _rootCache.Connect();
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
        if (!_locationIdToTree.TryGetValue(path.LocationId, out var tree))
        {
            tree = new TreeFolderGeneratorForLocationId<TTreeItemWithPath>();
            _locationIdToTree.Add(path.LocationId, tree);
            _rootCache.AddOrUpdate(tree.ModelForRoot());
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
        if (_locationIdToTree.TryGetValue(path.LocationId, out var tree))
        {
            var rootBecameEmpty = tree.OnDeleteFile(path.Path, itemModel);
            if (rootBecameEmpty)
            {
                _rootCache.Remove(tree.ModelForRoot());
                _locationIdToTree.Remove(path.LocationId);
            }
        }
    }
}
