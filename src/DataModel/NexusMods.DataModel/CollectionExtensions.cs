namespace NexusMods.DataModel;

/// <summary>
/// Extension methods tied to collections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Adds multiple items to a given hash set.
    /// </summary>
    /// <param name="hashSet">The collection to add the items to.</param>
    /// <param name="items">The items to add to the collection.</param>
    /// <typeparam name="T"></typeparam>
    public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
    {
        foreach (var item in items)
            hashSet.Add(item);
    }
}
