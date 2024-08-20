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
            .AutoRefreshOnObservable(itm => analyzerDatas.Where(set => set.Contains(itm.Id)))
            .RemoveKey();
    }
    
    /// <summary>
    /// Returns an IObservable of a loadout with the given id, refreshing it whenever a child entity is updated.
    /// </summary>
    public static IObservable<Loadout.ReadOnly> RevisionsWithChildUpdates(IConnection connection, LoadoutId id)
    {
        var analyzerDatas = connection.Revisions.Select(db => db.AnalyzerData<TreeAnalyzer, FrozenSet<EntityId>>());
        
        return analyzerDatas
            .Where(set => set.Contains(id))
            .Select(_ => Loadout.Load(connection.Db, id))
            .StartWith(Loadout.Load(connection.Db, id));
    }
}
