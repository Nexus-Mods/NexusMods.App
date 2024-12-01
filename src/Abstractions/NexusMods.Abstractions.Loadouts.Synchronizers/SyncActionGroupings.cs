using System.Collections.Concurrent;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A grouping of actions that can be performed on a file. The SyncTree has a `Actions`
/// field and that field is used to put the items in the tree into these specific groups.
/// </summary>
public class SyncActionGroupings<TNode>
where TNode : ISyncNode
{
    private readonly ConcurrentDictionary<Actions, ConcurrentBag<TNode>> _groupings = new();
    
    /// <summary>
    /// Gets the group of nodes that have the specified action.
    /// </summary>
    /// <param name="action"></param>
    public IReadOnlyCollection<TNode> this[Actions action] => _groupings.GetOrAdd(action, static _ => []);
    
    /// <summary>
    /// Adds a node to the groupings based on the actions it has.
    /// </summary>
    /// <param name="node"></param>
    public void Add(TNode node)
    {
        foreach (var flag in Enum.GetValues<Actions>())
        {
            if (node.Actions.HasFlag(flag))
                AddOne(flag, node);
        }
    }
    
    private void AddOne(Actions flag, TNode node)
    {
        _groupings.GetOrAdd(flag, static _ => []).Add(node);
    }
}
