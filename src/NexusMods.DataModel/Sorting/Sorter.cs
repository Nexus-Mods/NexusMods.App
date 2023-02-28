using System.Collections.Concurrent;
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
        int currentPrimeIndex = GetRuntimePrimeIndex(dict.Count);
        int prevPrime = RuntimePrimes[--currentPrimeIndex];
        var sorted = new List<TItem>(dict.Count);
        var used = new HashSet<TId>(dict.Count);
        
        var values = GC.AllocateUninitializedArray<(TId[] After, TItem Item)>(dict.Count);
        const int MultiThreadCutoff = 2000; // Based on benching with 5900X.
        const int MinOperationsPerThread = 666;
        ParallelOptions parallelOptions = default!;
        ParallelIsSuperset<TId, TItem> parallelState = default!;
        Action<Tuple<int, int>>? doWork = default;
        
        if (dict.Count > MultiThreadCutoff)
        {
            parallelOptions = new ParallelOptions();
            parallelState = new ParallelIsSuperset<TId, TItem>()
            {
                values = values,
                used = used,
                output = GC.AllocateUninitializedArray<(TId[] After, TItem Item)>(dict.Count)
            };
            doWork = parallelState.DoWork;
        }
        
        while (dict.Count > 0)
        {
            // Copy to values (no alloc), then filter them in-place
            dict.Values.CopyTo(values, 0); // <= Bottleneck. 
            int superSetSize = 0;
            var valuesSlice = values.AsSpan(0, dict.Count);
            if (dict.Count > MultiThreadCutoff)
            {
                parallelOptions.MaxDegreeOfParallelism = Math.Max(dict.Count / MinOperationsPerThread, 1);
                parallelState.superSetSize = -1;
                Parallel.ForEach(Partitioner.Create(0, valuesSlice.Length), parallelOptions, doWork);
                superSetSize = parallelState.NumberOfElements;
                valuesSlice = parallelState.output.AsSpan(0, superSetSize); // <= assign output span.
            }
            else
            {
                // Note: For Span<T>, foreach is lowered to for; there is no enumerator allocation.
                foreach (var value in valuesSlice)
                {
                    if (IsSupersetOf(used, value.After))
                        values[superSetSize++] = value;
                }
            }

            // Slice to our superset.
            valuesSlice = valuesSlice.Slice(0, superSetSize);
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
            
            // This is pretty stupid, but it's fastest way to work around the huge bottleneck pulling values.
            // It'd be nice if the runtime had that one loop unrolled. :), maybe I'll PR it.
            if (dict.Count < prevPrime)
            {
                dict = new Dictionary<TId, (TId[] After, TItem Item)>(dict);
                prevPrime = RuntimePrimes[--currentPrimeIndex];
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
                            idsBuffer.Add(otherId);
                        break;
                    case After<TItem, TId> after:
                        // Handled above
                        break;
                    case Before<TItem, TId> b:
                        if (b.Other.Equals(idForThisItem))
                            idsBuffer.Add(otherId);
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
        internal (TId[] After, TItem Item)[] output;
        internal int superSetSize = -1; // after first thread safe increment this is 0
        internal int NumberOfElements => superSetSize + 1;

        internal void DoWork(Tuple<int, int> tuple)
        {
            // Work on our slice.
            var val = values;
            var otp = output;
            var used = this.used;
            for (int x = tuple.Item1; x < tuple.Item2; x++)
            {
                var value = val[x];
                if (!IsSupersetOf(used, value.After))
                    continue;

                otp[Interlocked.Increment(ref superSetSize)] = value;
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

    // List of primes preferred by the runtime as dictionary sizes.
    // These haven't changed in at least ~15 years.
    private static int GetRuntimePrimeIndex(int value)
    {
        for (int x = 0; x < RuntimePrimes.Length; x++)
        {
            var prime = RuntimePrimes[x];
            if (prime >= value)
                return x;
        }

        return RuntimePrimes.Length - 1;
    }
    
    private static readonly int[] RuntimePrimes =
    {
        -1, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 
        3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 
        43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371, 324449, 
        389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263, 1674319, 2009191, 2411033, 
        2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    };
}