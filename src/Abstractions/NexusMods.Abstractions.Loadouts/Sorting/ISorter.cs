namespace NexusMods.Abstractions.Loadouts.Sorting;

/// <summary>
///     Provides sorting capabilities for mods.
/// </summary>
public interface ISorter
{
    /// <summary>
    /// Sorts a collection of user items.
    /// </summary>
    /// <param name="items">The items to process.</param>
    /// <param name="idSelector">Function that extracts an individual id from an item.</param>
    /// <param name="ruleFn">Function that pulls a list of rules for a specific item.</param>
    /// <param name="comparer">[Optional] Comparer for items.</param>
    /// <typeparam name="TItem">The type of item used.</typeparam>
    /// <typeparam name="TId">The type of ID used for the item.</typeparam>
    /// <returns>Sorted collection of items.</returns>
    IEnumerable<TItem> SortWithEnumerable<TItem, TId>(IEnumerable<TItem> items,
        Func<TItem, TId> idSelector,
        Func<TItem, IReadOnlyList<ISortRule<TItem, TId>>> ruleFn,
        IComparer<TId>? comparer = null)
        where TId : IEquatable<TId>;

    /// <summary>
    /// Sorts a collection of user items.
    /// </summary>
    /// <param name="items">The items to process.</param>
    /// <param name="idSelector">Function that extracts an individual id from an item.</param>
    /// <param name="ruleFn">Function that pulls a list of rules for a specific item.</param>
    /// <param name="comparer">[Optional] Comparer for items.</param>
    /// <typeparam name="TItem">The type of item used.</typeparam>
    /// <typeparam name="TId">The type of ID used for the item.</typeparam>
    /// <returns>Sorted collection of items.</returns>
    IEnumerable<TItem> Sort<TItem, TId>(List<TItem> items,
        Func<TItem, TId> idSelector,
        Func<TItem, IReadOnlyList<ISortRule<TItem, TId>>> ruleFn,
        IComparer<TId>? comparer = null)
        where TId : IEquatable<TId>;

    /// <summary>
    /// Sorts a collection of user items.
    /// </summary>
    /// <param name="items">The items to process.</param>
    /// <param name="idSelector">Function that extracts an individual id from an item.</param>
    /// <param name="ruleFn">Function that pulls a list of rules for a specific item.</param>
    /// <param name="comparer">[Optional] Comparer for items.</param>
    /// <typeparam name="TItem">The type of item used.</typeparam>
    /// <typeparam name="TId">The type of ID used for the item.</typeparam>
    /// <typeparam name="TCollection">Type of collection used.</typeparam>
    /// <returns>Sorted collection of items.</returns>
    IEnumerable<TItem> Sort<TItem, TId, TCollection>(TCollection items,
        Func<TItem, TId> idSelector,
        Func<TItem, IReadOnlyList<ISortRule<TItem, TId>>> ruleFn,
        IComparer<TId>? comparer = null)
        where TId : IEquatable<TId>
        where TCollection : class, IReadOnlyList<TItem>;
}
