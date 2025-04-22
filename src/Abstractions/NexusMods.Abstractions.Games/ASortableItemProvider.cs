using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <inheritdoc />
public abstract class ASortableItemProvider : ILoadoutSortableItemProvider
{
    private bool _isDisposed;
    
    /// <summary>
    /// Async semaphore for serializing changes to the sort order
    /// </summary>
    protected readonly SemaphoreSlim Semaphore = new(1, 1);
    
    /// <summary>
    /// Source cache of the sortable items used to expose the latest sort order
    /// </summary>
    protected readonly SourceCache<ISortableItem, ISortItemKey> OrderCache = new(item => item.Key);
    
    /// <summary>
    /// Protected constructor, use CreateAsync method to create an instance
    /// </summary>
    protected ASortableItemProvider(ISortableItemProviderFactory parentFactory, LoadoutId loadoutId)
    {
        ParentFactory = parentFactory;
        LoadoutId = loadoutId;
    }

    /// <inheritdoc />
    public ISortableItemProviderFactory ParentFactory { get; }

    /// <inheritdoc />
    public LoadoutId LoadoutId { get; }

    /// <inheritdoc />
    public abstract ReadOnlyObservableCollection<ISortableItem> SortableItems { get; }

    /// <inheritdoc />
    public abstract IObservable<IChangeSet<ISortableItem, ISortItemKey>> SortableItemsChangeSet { get; }

    /// <Inheritdoc />
    public abstract Optional<ISortableItem> GetSortableItem(ISortItemKey itemId);
    
    /// <Inheritdoc />
    public abstract Task SetRelativePosition(ISortableItem sortableItem, int delta, CancellationToken token);

    /// <Inheritdoc />
    public abstract Task MoveItemsTo(ISortableItem[] sourceItems, ISortableItem targetItem, TargetRelativePosition relativePosition, CancellationToken token);
    

    /// <inheritdoc />
    public virtual void Dispose()
    {
        Dispose(true);
    }
    
    /// <summary>
    /// Disposes base class
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            Semaphore.Dispose();
        }

        _isDisposed = true;
    }
    
}
