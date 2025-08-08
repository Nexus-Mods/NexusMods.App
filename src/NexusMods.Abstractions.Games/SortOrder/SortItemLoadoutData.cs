using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Default implementation to cover most use cases of <see cref="ISortItemLoadoutData"/>.
/// <inheritdoc cref="ISortItemLoadoutData"/>
/// </summary>
public class SortItemLoadoutData<TKey> : ISortItemLoadoutData<TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    /// <summary>
    /// Constructs a new instance of <see cref="SortItemLoadoutData{TKey}"/>.
    /// </summary>
    public SortItemLoadoutData(TKey key, bool isEnabled, string modName, Optional<LoadoutItemGroupId> modGroupId)
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
