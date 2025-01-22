using DynamicData;

namespace NexusMods.App.UI.Extensions;

public class SourceCacheAdapter<TObject, TKey> : IChangeSetAdaptor<TObject, TKey>
    where TObject : notnull
    where TKey : notnull
{
    private readonly ISourceCache<TObject, TKey> _sourceCache;

    public SourceCacheAdapter(ISourceCache<TObject, TKey> sourceCache)
    {
        _sourceCache = sourceCache;
    }

    public void Adapt(IChangeSet<TObject, TKey> changes)
    {
        _sourceCache.Edit(updater =>
        {
            foreach (var change in changes)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                        updater.AddOrUpdate(change.Current, change.Key);
                        break;
                    case ChangeReason.Update:
                        updater.AddOrUpdate(change.Current, change.Key);
                        break;
                    case ChangeReason.Remove:
                        updater.RemoveKey(change.Key);
                        break;
                    case ChangeReason.Refresh:
                        updater.Refresh(change.Key);
                        break;
                    case ChangeReason.Moved:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        });
    }
}
