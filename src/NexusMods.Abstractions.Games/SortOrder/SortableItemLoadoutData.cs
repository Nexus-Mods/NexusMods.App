using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Default implementation to cover most use cases of <see cref="ISortableItemLoadoutData"/>.
/// <inheritdoc cref="ISortableItemLoadoutData"/>
/// </summary>
public class SortableItemLoadoutData<TKey> : ISortableItemLoadoutData<TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    /// <summary>
    /// Constructs a new instance of <see cref="SortableItemLoadoutData"/>.
    /// </summary>
    public SortableItemLoadoutData(TKey key, bool isEnabled, string modName, Optional<LoadoutItemGroupId> modGroupId)
    {
        Key = key;
        IsEnabled = isEnabled;
        ModName = modName;
        ModGroupId = modGroupId;
    }
    
    /// <inheritdoc />
    public TKey Key { get; }
    
    /// <inheritdoc />
    public bool IsEnabled { get; set; }
    
    /// <inheritdoc />
    public string ModName { get; set; }
    
    /// <inheritdoc />
    public Optional<LoadoutItemGroupId> ModGroupId { get; set; }
}
