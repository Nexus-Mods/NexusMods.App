using System.Collections.Frozen;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.MnemonicDB.Analyzers;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Loadouts;

public partial class Loadout
{
    /// <summary>
    /// Returns all the loadouts in the database, refreshing them whenever a child entity is updated.
    /// </summary>
    public static IObservable<IChangeSet<Loadout.ReadOnly>> ObserveAllWithChildUpdates(IConnection connection)
    {
        var analyzerDatas = connection.Revisions.Select(db => db.AnalyzerData<TreeAnalyzer, FrozenSet<EntityId>>());
        
        return Loadout.ObserveAll(connection)
            .AutoRefreshOnObservable(itm => analyzerDatas.Where(set => set.Contains(itm.Id)));
    }
}
