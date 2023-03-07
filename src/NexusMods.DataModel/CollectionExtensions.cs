namespace NexusMods.DataModel;

public static class CollectionExtensions
{
    public static void AddRange<T>(this HashSet<T> coll, IEnumerable<T> itms)
    {
        foreach (var itm in itms)
            coll.Add(itm);
    }

}
