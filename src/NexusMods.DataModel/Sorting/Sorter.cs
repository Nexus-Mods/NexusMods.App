using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Sorting;

public class Sorter
{
    public static IEnumerable<TItem> Sort<TItem, TId>(IEnumerable<TItem> items,
        Func<TItem, IEnumerable<ISortRule<TItem, TId>>> ruleFn) 
        where TItem : IHasEntityId<TId> 
        where TId : IEquatable<TId>
    {
        var indexed = items
            .AsParallel()
            .Select(i => (After: RefineRules(i, ruleFn, items), Item: i))
            .ToDictionary(i => i.Item.Id, i => i);

        var sorted = new List<TItem>();
        var used = new HashSet<TId>();
        
        while (indexed.Count > 0)
        {
            var fit = indexed.Values.Where(v => used.IsSupersetOf(v.After));
            var found = false;

            foreach (var (_, item) in fit)
            {
                found = true;
                sorted.Add(item);
                used.Add(item.Id);
                indexed.Remove(item.Id);
            }

            if (!found)
                throw new InvalidOperationException("Cyclic dependency detected");
        }

        return sorted;
    }

    private static HashSet<TId> RefineRules<TItem, TId>(TItem thisItem, 
        Func<TItem, IEnumerable<ISortRule<TItem, TId>>> ruleFn, 
        IEnumerable<TItem> items) where TItem : IHasEntityId<TId>
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
            if (itm.Id.Equals(thisItem.Id))
                continue;
            
            foreach (var rule in ruleFn(itm))
            {
                switch (rule)
                {
                    case First<TItem, TId>:
                        if (!haveFirst) 
                            ids.Add(itm.Id);
                        break;
                    case After<TItem, TId> after:
                        // Handled above
                        break;
                    case Before<TItem, TId> b:
                        if (b.Other.Equals(thisItem.Id))
                            ids.Add(itm.Id);
                        break;
                }
            }
        }
        
        return ids;
    }
}