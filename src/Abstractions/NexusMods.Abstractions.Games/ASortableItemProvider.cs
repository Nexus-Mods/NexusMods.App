using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <inheritdoc />
public abstract class ASortableItemProvider : ILoadoutSortableItemProvider
{
    
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
    public abstract IObservable<IChangeSet<ISortableItem, Guid>> SortableItemsChangeSet { get; }

    /// <Inheritdoc />
    public abstract Optional<ISortableItem> GetSortableItem(Guid itemId);
    
    /// <Inheritdoc />
    public abstract Task SetRelativePosition(ISortableItem sortableItem, int delta, CancellationToken token);

    /// <Inheritdoc />
    public abstract Task MoveItemsTo(ISortableItem[] sourceItems, ISortableItem targetItem, TargetRelativePosition relativePosition, CancellationToken token);

    /// <inheritdoc />
    public abstract void Dispose();

}
