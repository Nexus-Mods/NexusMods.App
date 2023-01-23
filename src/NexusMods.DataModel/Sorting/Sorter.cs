using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Sorting;

public class Sorter
{
    public static IEnumerable<TItem> Sort<TItem, TId>(IEnumerable<TItem> items,
        Func<TItem, TId> idSelector,
        Func<TItem, IEnumerable<ISortRule<TItem, TId>>> ruleFn,
        IComparer<TId>? comparer = null) 
        where TId : IEquatable<TId>
    {
        var indexed = items
            .AsParallel()
            .Select(i => (After: RefineRules(i, idSelector, ruleFn, items), Item: i))
            .ToDictionary(i => idSelector(i.Item), i => i);

        var sorted = new List<TItem>();
        var used = new HashSet<TId>();
        
        while (indexed.Count > 0)
        {
            var fit = indexed.Values.Where(v => used.IsSupersetOf(v.After));
            if (comparer != null)
                fit = fit.OrderBy(x => idSelector(x.Item), comparer);
            var found = false;

            foreach (var (_, item) in fit)
            {
                var id = idSelector(item);
                found = true;
                sorted.Add(item);
                used.Add(id);
                indexed.Remove(id);
            }

            if (!found)
                throw new InvalidOperationException("Cyclic dependency detected");
        }

        return sorted;
    }

    private static HashSet<TId> RefineRules<TItem, TId>(TItem thisItem, 
        Func<TItem, TId> idSelector,
        Func<TItem, IEnumerable<ISortRule<TItem, TId>>> ruleFn, 
        IEnumerable<TItem> items)
    where TId : IEquatable<TId>
    {
        var rules = ruleFn(thisItem).ToList();
        var ids = new HashSet<TId>();
        bool haveFirst = false;
        foreach (var rule in rules)
        {
            switch (rule)
            {
                case First<TItem, TId>:
                    // Handled later
                    haveFirst = true;
                    break;
                case After<TItem, TId> after:
                    ids.Add(after.Other);
                    break;
                case Before<TItem, TId>:
                    // Handled later
                    break;
            }
        }
        
        foreach (var itm in items)
        {
            if (idSelector(itm).Equals(idSelector(thisItem)))
                continue;
            
            foreach (var rule in ruleFn(itm))
            {
                switch (rule)
                {
                    case First<TItem, TId>:
                        if (!haveFirst) 
                            ids.Add(idSelector(itm));
                        break;
                    case After<TItem, TId> after:
                        // Handled above
                        break;
                    case Before<TItem, TId> b:
                        if (b.Other.Equals(idSelector(thisItem)))
                            ids.Add(idSelector(itm));
                        break;
                }
            }
        }
        
        return ids;
    }
}