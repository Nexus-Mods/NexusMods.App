using DynamicData;

namespace NexusMods.App.UI.Extensions;

public class ListAdapter<T> : IChangeSetAdaptor<T> where T : notnull
{
    private readonly IList<T> _source;
    public ListAdapter(IList<T> source)
    {
        _source = source;
    }

    public void Adapt(IChangeSet<T> change)
    {
        _source.Clone(change);
    }
}
