using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.Generic.Extensions;

public static class LibraryArchiveTreeExtensions
{
    public static IEnumerable<KeyedBox<TKey, TSelf>> EnumerateFilesBfsWhereBranch<TSelf, TKey>(
        this KeyedBox<TKey, TSelf> item,
        Func<KeyedBox<TKey, TSelf>, bool> predicate)
        where TKey : notnull
        where TSelf : struct, IHaveAFileOrDirectory, IHaveBoxedChildrenWithKey<TKey, TSelf>, IHaveKey<TKey>
    {
        var queue = new Queue<KeyedBox<TKey, TSelf>>();
        foreach (var child in item.Children())
        {
            queue.Enqueue(child.Value);
        }

        while (queue.TryDequeue(out var current))
        {
            if (!predicate(current)) continue;

            if (current.IsFile())
            {
                yield return current;
            }

            foreach (var grandChild in current.Item.Children)
                queue.Enqueue(grandChild.Value);
        }
    }
}
