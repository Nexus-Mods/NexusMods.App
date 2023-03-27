using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// Provides the main entry point for listing, modifying and updating loadouts.
/// </summary>
public class LoadoutRegistry
{
    private readonly ILogger<LoadoutRegistry> _logger;
    private readonly IDataStore _store;

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="store"></param>
    public LoadoutRegistry(ILogger<LoadoutRegistry> logger, IDataStore store)
    {
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// Alters the loadout with the given id. If the loadout does not exist, it is created.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterFn"></param>
    /// <returns></returns>
    public Loadout Alter(LoadoutId id, string commitMessage, Func<Loadout, Loadout> alterFn)
    {
        var loadoutRoot = _store.GetRaw(id.ToEntityId(EntityCategory.LoadoutRoots));
        Loadout? loadout = null;
        if (loadoutRoot != null)
        {
            loadout = _store.Get<Loadout>(IId.FromTaggedSpan(loadoutRoot));
        }

        var newLoadout = alterFn(loadout ?? Loadout.Empty(_store));
        newLoadout.EnsurePersisted(_store);

        Span<byte> span = stackalloc byte[newLoadout.DataStoreId.SpanSize + 1];
        newLoadout.DataStoreId.ToTaggedSpan(span);
        // TODO: Make this atomic
        _store.PutRaw(id.ToEntityId(EntityCategory.LoadoutRoots), span);

        _logger.LogInformation("Loadout {LoadoutId} altered: {CommitMessage}", id, commitMessage);

        return newLoadout;
    }

    /// <summary>
    /// Alters the mod with the given id in the loadout with the given id. If the alter
    /// function returns null, the mod is removed from the loadout.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterfn"></param>
    public void Alter(LoadoutId loadoutId, ModId modId, string commitMessage, Func<Mod?, Mod?> alterfn)
    {
        Alter(loadoutId, commitMessage, loadout =>
        {
            var existingMod = loadout.Mods.TryGetValue(modId, out var mod) ? mod : null;
            var newMod = alterfn(existingMod);
            if (newMod == null)
            {
                if (existingMod != null)
                {
                    return loadout with { Mods = loadout.Mods.Without(modId) };
                }
            }
            else
            {
                return loadout with { Mods = loadout.Mods.With(modId, newMod) };
            }
            return loadout;
        });

    }

    /// <summary>
    /// Gets the loadout with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Loadout? Get(LoadoutId id)
    {
        var loadoutRoot = _store.GetRaw(id.ToEntityId(EntityCategory.LoadoutRoots));
        if (loadoutRoot == null)
        {
            throw new InvalidOperationException($"Loadout {id} does not exist");
        }

        return _store.Get<Loadout>(IId.FromTaggedSpan(loadoutRoot));
    }

    /// <summary>
    /// Gets the mod with the given id from the given loadout.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <returns></returns>
    public Mod? Get(LoadoutId loadoutId, ModId modId)
    {
        var loadout = Get(loadoutId);
        return loadout?.Mods[modId];
    }

    /// <summary>
    /// Returns all loadout ids.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<LoadoutId> All()
    {
        return _store.AllIds(EntityCategory.LoadoutRoots)
                .Select(LoadoutId.From);
    }

    public IObservable<IChangeSet<LoadoutId>>


}
