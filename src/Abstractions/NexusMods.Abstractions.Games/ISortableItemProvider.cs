using System.Collections.ObjectModel;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// A provider of sortable items, these are often things like game plugins or mods that the game can load natively.
/// </summary>
public interface ISortableItemProvider
{
    public ReadOnlyObservableCollection<ISortableItem> Items { get; }
}
