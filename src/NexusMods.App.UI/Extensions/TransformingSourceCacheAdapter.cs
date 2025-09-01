using System.Collections.Concurrent;
using DynamicData;

namespace NexusMods.App.UI.Extensions;

/// <summary>
/// Adapter that transforms both values and keys before applying changes to a target source cache.
/// Maintains internal key mapping to handle removals correctly.
/// </summary>
/// <typeparam name="TSource">The source item type</typeparam>
/// <typeparam name="TSourceKey">The source key type</typeparam>
/// <typeparam name="TTarget">The target item type</typeparam>
/// <typeparam name="TTargetKey">The target key type</typeparam>
/// <param name="targetCache">The target cache to apply changes to</param>
/// <param name="transform">Function to transform source items to target items</param>
/// <param name="keySelector">Function to extract target keys from source items</param>
public class TransformingSourceCacheAdapter<TSource, TSourceKey, TTarget, TTargetKey>(
    ISourceCache<TTarget, TTargetKey> targetCache,
    Func<TSource, TTarget> transform,
    Func<TSource, TTargetKey> keySelector)
    : IChangeSetAdaptor<TSource, TSourceKey>
    where TSource : notnull
    where TSourceKey : notnull
    where TTarget : notnull
    where TTargetKey : notnull
{
    private readonly ConcurrentDictionary<TSourceKey, TTargetKey> _keyMapping = new();

    public void Adapt(IChangeSet<TSource, TSourceKey> changes)
    {
        targetCache.Edit(updater =>
        {
            foreach (var change in changes)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                    case ChangeReason.Refresh:
                        var target = transform(change.Current);
                        var targetKey = keySelector(change.Current);
                        _keyMapping.AddOrUpdate(change.Key, targetKey, (_, _) => targetKey);
                        updater.AddOrUpdate(target, targetKey);
                        break;
                    case ChangeReason.Remove:
                        if (_keyMapping.TryRemove(change.Key, out var mappedKey))
                            updater.RemoveKey(mappedKey);
                        break;
                    case ChangeReason.Moved:
                        // For moves, we don't need to do anything special with the mapping
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        });
    }
}
