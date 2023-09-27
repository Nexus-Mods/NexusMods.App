using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// A delegating tree is a tree that wraps another tree and delegates all calls to it.
/// </summary>
/// <typeparam name="TPath"></typeparam>
/// <typeparam name="TEntry"></typeparam>
public class AGamePathTree<TEntry>
{

    private readonly Dictionary<LocationId, FileTreeNode<GamePath, TEntry>> _trees;

    /// <summary>
    /// Base constructor for a delegating tree, this is used to wrap a tree and delegate all calls to it.
    /// </summary>
    /// <param name="tree"></param>
    protected AGamePathTree(IEnumerable<KeyValuePair<GamePath, TEntry>> items)
    {
        _trees = items.GroupBy(i => i.Key.LocationId)
            .Select(g => (g.Key, FileTreeNode<GamePath, TEntry>.CreateTree(g)))
            .ToDictionary(d => d.Key, d => d.Item2);
    }


    /// <summary>
    /// Returns all the files in this tree
    /// </summary>
    /// <returns></returns>
    public IEnumerable<FileTreeNode<GamePath, TEntry>> GetAllDescendentFiles()
    {
        return _trees.Values.SelectMany(e => e.GetAllDescendentFiles());
    }

    /// <summary>
    /// Returns all the files in a specific location
    /// </summary>
    /// <param name="id"></param>
    public FileTreeNode<GamePath, TEntry> this[LocationId id] => _trees[id];

    /// <summary>
    /// Returns all the files in a specific location
    /// </summary>
    /// <param name="path"></param>
    public FileTreeNode<GamePath, TEntry> this[GamePath path] => _trees[path.LocationId].FindNode(path)!;

    /// <summary>
    /// Attempts to get a value from the tree, returns false if the value does not exist.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValue(LocationId id, [MaybeNullWhen(false)] out FileTreeNode<GamePath, TEntry> value)
    {
        return _trees.TryGetValue(id, out value);
    }

    /// <summary>
    /// Attempts to get a value from the tree, returns false if the value does not exist.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValue(GamePath path, [MaybeNullWhen(false)] out FileTreeNode<GamePath, TEntry> value)
    {
        if (_trees.TryGetValue(path.LocationId, out var tree))
        {
            var tmpValue = tree.FindNode(path);
            if (tmpValue != null)
            {
                value = tmpValue;
                return true;
            }
        }

        value = default;
        return false;
    }
}
