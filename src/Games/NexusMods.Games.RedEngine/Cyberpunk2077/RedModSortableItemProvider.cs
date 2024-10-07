using System.Collections.ObjectModel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;


public class RedModSortableItemProvider : ISortableItemProvider
{
    private readonly LoadoutId _loadoutId;
    private readonly IConnection _conn;


    public RedModSortableItemProvider(IConnection conn, LoadoutId loadoutId)
    {
        _conn = conn;
        _loadoutId = loadoutId;
    }
    
    public ReadOnlyObservableCollection<ISortableItem> Items { get; } = new(new ObservableCollection<ISortableItem>());
}
