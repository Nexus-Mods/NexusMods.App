using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.MnemonicDB.Analyzers;

namespace NexusMods.Abstractions.Loadouts;

public partial class LoadoutObservables
{
    /// <summary>
    /// Returns all the loadouts in the database, refreshing them whenever a child entity is updated.
    /// </summary>
    public static IObservable<IChangeSet<Loadout.ReadOnly>> AllObservable(ITreeAnalyzer analyzer)
    {
        return Loadout.ObserveAll(analyzer.Connection)
            .Select(itm => itm)
            .AutoRefreshOnObservable(itm => analyzer.Updates.Where(u => u.Updated.Contains(itm.Id)))
            .Select(itm => itm);
    }
}
