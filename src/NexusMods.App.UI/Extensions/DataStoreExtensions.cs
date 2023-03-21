using System.Reactive.Linq;
using DynamicData;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;

namespace NexusMods.App.UI.Extensions;

public static class DataStoreExtensions
{
    private static TimeSpan ThrottleRate = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Returns an observable that emits the loadouts in the data store. And updates
    /// every time a loadout is added, removed, or changed.
    /// </summary>
    /// <param name="store"></param>
    /// <returns></returns>
    public static IObservable<IChangeSet<Loadout, LoadoutId>> ObservableLoadouts(this IDataStore store)
    {
        return store.RootChanges
            .Where(rootChange =>
                rootChange.To.Category == EntityCategory.Loadouts)
            .Select(rootChange => rootChange.To)
            .StartWith(store.GetRoot(RootType.Loadouts))
            .WhereNotNull()
            .Throttle(ThrottleRate)
            .Select(id => store.Get<LoadoutRegistry>(id, true))
            .WhereNotNull()
            .Select(registry => registry.Lists.Values)
            .ToObservableChangeSet(loadout => loadout.LoadoutId)
            .OnUI();
    }

    /// <summary>
    /// Returns an observable that emits the games in the data store. And updates
    /// every time a loadout is added, removed, or changed.
    /// </summary>
    /// <param name="store"></param>
    /// <returns></returns>
    public static IObservable<IDistinctChangeSet<IGame>> ObservableManagedGames(
        this IDataStore store)
    {
        return store.ObservableLoadouts()
            .DistinctValues(loadout => loadout.Installation.Game);
    }
}
