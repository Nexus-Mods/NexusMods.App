using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Sorting;

/// <summary>
/// Utility class for sorting mods according to a set of defined rules.
/// </summary>
public class Sorter
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
    public static IEnumerable<TItem> SortWithEnumerable<TItem, TId>(IEnumerable<TItem> items,
        Func<TItem, TId> idSelector,
        Func<TItem, IReadOnlyList<ISortRule<TItem, TId>>> ruleFn,
        IComparer<TId>? comparer = null)
        where TId : IEquatable<TId>
    {
        /*
             Observation from me (Sewer).  
             
             In practice our mods/items are fetched from an IDataStore.  
             
             At the current moment in time, this is backed by an in memory database, 
             so access times should be relatively quick and thus calling `.ToArray()` should be fine.  
             
             That said, if this data ever was to be file backed, adding an asynchronous access method
             might not be too bad.
        */
        return Sort(items.ToArray(), idSelector, ruleFn, comparer);
    }
    
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
    public static IEnumerable<TItem> Sort<TItem, TId>(List<TItem> items,
        Func<TItem, TId> idSelector,
        Func<TItem, IReadOnlyList<ISortRule<TItem, TId>>> ruleFn,
        IComparer<TId>? comparer = null) 
        where TId : IEquatable<TId>
    {
        return Sort<TItem, TId, List<TItem>>(items, idSelector, ruleFn, comparer);
    }

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
    public static IEnumerable<TItem> Sort<TItem, TId, TCollection>(TCollection items,
        Func<TItem, TId> idSelector,
        Func<TItem, IReadOnlyList<ISortRule<TItem, TId>>> ruleFn,
        IComparer<TId>? comparer = null) 
        where TId : IEquatable<TId>
        where TCollection : class, IReadOnlyList<TItem>
    {
        var partitioner = Partitioner.Create(0, items.Count);
        var indexed = new ConcurrentDictionary<TId, (TId[] After, TItem Item)>(Environment.ProcessorCount, items.Count);
        
        Parallel.ForEach(partitioner, tuple =>
        {
            var idBuffer = new HashSet<TId>(tuple.Item2 - tuple.Item1);
            
            // Work on our slice.
            for (int x = tuple.Item1; x < tuple.Item2; x++)
            {
                var item = items[x];
                var rules = RefineRules(item, idSelector, ruleFn, items, idBuffer);
                indexed[idSelector(item)] = (rules, item);
            }
        });

        // This copy is necessary because ConcurrentDictionary does not have required APIs
        // for fast value access, and that bottlenecks us. No better option without custom dict.
        var dict = new Dictionary<TId, (TId[] After, TItem Item)>(indexed);
        var sorted = new List<TItem>(dict.Count);
        var used = new HashSet<TId>(dict.Count);
        
        // Note: Dictionary does not expose this API publicly so a cast here is needed.
        var values = GC.AllocateUninitializedArray<(TId[] After, TItem Item)>(dict.Count);
        var parallelOptions = new ParallelOptions();
        var parallelState = new ParallelIsSuperset<TId, TItem>()
        {
            values = values,
            used = used
        };
        
        var doWork = parallelState.DoWork;
        const int MultiThreadCutoff = 2000; // Based on benching with 5900X.
        const int MinOperationsPerThread = 666;
        
        while (dict.Count > 0)
        {
            // Copy to values (no alloc), then filter them in-place
            // Note: For Span<T>, foreach is lowered to for; there is no enumerator allocation.
            dict.Values.CopyTo(values, 0);
            int superSetSize = 0;
            var valuesSlice = values.AsSpan(0, dict.Count);
            if (dict.Count > MultiThreadCutoff)
            {
                parallelOptions.MaxDegreeOfParallelism = Math.Max(dict.Count / MinOperationsPerThread, 1);
                parallelState.superSetSize = 0;
                Parallel.ForEach(Partitioner.Create(0, valuesSlice.Length), parallelOptions, doWork);
                superSetSize = parallelState.superSetSize;
            }
            else
            {
                foreach (var value in valuesSlice)
                {
                    if (IsSupersetOf(used, value.After))
                        values[superSetSize++] = value;
                }
            }

            // Slice to our superset.
            valuesSlice = values.AsSpan(0, superSetSize);
            if (comparer != null)
            {
                valuesSlice.Sort((a, b) =>
                {
                    var aId = idSelector(a.Item);
                    var bId = idSelector(b.Item);
                    return comparer.Compare(aId, bId);
                });
            }
            
            var found = false;

            foreach (var value in valuesSlice)
            {
                var id = idSelector(value.Item);
                found = true;
                sorted.Add(value.Item);
                used.Add(id);
                dict.Remove(id, out _);
            }

            if (!found)
                throw new InvalidOperationException("Cyclic dependency detected");
        }

        return sorted;
    }

    private static TId[] RefineRules<TItem, TId>(TItem thisItem, 
        Func<TItem, TId> idSelector,
        Func<TItem, IReadOnlyList<ISortRule<TItem, TId>>> ruleFn, 
        IReadOnlyList<TItem> items,
        HashSet<TId> idsBuffer)
    where TId : IEquatable<TId>
    {
        idsBuffer.Clear();
        bool haveFirst = false;
        var rulesForThisItem = ruleFn(thisItem);
        
        /*
             Please do not refactor 'for' as foreach in this method.
             That will lead to enumerator allocation, which in turn is a 
             lot of allocations made; as this is O(N^2)
             
             - Sewer
        */
        for (var x = 0; x < rulesForThisItem.Count; x++)
        {
            switch (rulesForThisItem[x])
            {
                case First<TItem, TId>:
                    // Handled later
                    haveFirst = true;
                    break;
                case After<TItem, TId> after:
                    idsBuffer.Add(after.Other);
                    break;
                case Before<TItem, TId>:
                    // Handled later
                    break;
            }
        }

        var idForThisItem = idSelector(thisItem);
        for (var x = 0; x < items.Count; x++)
        {
            var itm = items[x];
            var otherId = idSelector(itm);
            if (otherId.Equals(idForThisItem))
                continue;

            var rulesForOtherItem = ruleFn(itm);
            for (var y = 0; y < rulesForOtherItem.Count; y++)
            {
                var rule = rulesForOtherItem[y];
                switch (rule)
                {
                    case First<TItem, TId>:
                        if (!haveFirst)
                            idsBuffer.Add(idSelector(itm));
                        break;
                    case After<TItem, TId> after:
                        // Handled above
                        break;
                    case Before<TItem, TId> b:
                        if (b.Other.Equals(idSelector(thisItem)))
                            idsBuffer.Add(idSelector(itm));
                        break;
                }
            }
        }

        return idsBuffer.ToArray();
    }

    /// <summary>
    /// Holds state for the delegate invoked for parallel computation of IsSupersetOf.
    /// To allow multiple calls to Parallel.For without expensive captures.
    /// </summary>
    private class ParallelIsSuperset<TId, TItem>
    {
        internal HashSet<TId> used;
        internal (TId[] After, TItem Item)[] values;
        internal int superSetSize;

        internal void DoWork(Tuple<int, int> tuple)
        {
            // Work on our slice.
            var val = values;
            var used = this.used;
            for (int x = tuple.Item1; x < tuple.Item2; x++)
            {
                var value = val[x];
                if (!IsSupersetOf(used, value.After))
                    continue;

                lock (this)
                    val[superSetSize++] = value;
            }
        }
    }

    #region Modified Runtime Code for No Alloc
    /// <summary>Determines whether a <see cref="HashSet{T}"/> object is a proper superset of the specified collection.</summary>
    /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
    /// <returns>true if the <see cref="HashSet{T}"/> object is a superset of <paramref name="other"/>; otherwise, false.</returns>
    public static bool IsSupersetOf<T>(HashSet<T> set, T[] other)
    {
        // If other is the empty set then this is a superset.
        if (other.Length == 0)
            return true;

        return ContainsAllElements(set, other);
    }
    
    /// <summary>
    /// Checks if this contains of other's elements. Iterates over other's elements and
    /// returns false as soon as it finds an element in other that's not in this.
    /// </summary>
    private static bool ContainsAllElements<T>(HashSet<T> set, T[] other)
    {
        // Although Roslyn should lower this into for loop, please don't rewrite as foreach.
        for (var x = 0; x < other.Length; x++)
        {
            if (!set.Contains(other[x]))
                return false;
        }

        return true;
    }
    #endregion
}