using System.Collections.Concurrent;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A grouping of actions that can be performed on a file. The SyncTree has a `Actions`
/// field and that field is used to put the items in the tree into these specific groups.
/// </summary>
public class SyncActionGroupings
{
    private ConcurrentDictionary<Actions, ConcurrentBag<SyncTreeNodeOld>> _groupings = new();
    
    /// <summary>
    /// Gets the group of nodes that have the specified action.
    /// </summary>
    /// <param name="action"></param>
    public IReadOnlyCollection<SyncTreeNodeOld> this[Actions action] => _groupings.GetOrAdd(action, static _ => []);
    
    /// <summary>
    /// Adds a node to the groupings based on the actions it has.
    /// </summary>
    /// <param name="node"></param>
    public void Add(SyncTreeNodeOld node)
    {
        foreach (var flag in Enum.GetValues<Actions>())
        {
            if (node.Actions.HasFlag(flag))
                AddOne(flag, node);
        }
    }
    
    private void AddOne(Actions flag, SyncTreeNodeOld node)
    {
        _groupings.GetOrAdd(flag, static _ => []).Add(node);
    }
}
