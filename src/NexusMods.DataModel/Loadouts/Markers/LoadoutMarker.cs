using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Loadouts.Markers;

/// <summary>
/// Represents a mutable marker of a loadout. Operations on this class
/// may mutate a loadout, which will then cause a "rebase" of this marker
/// on the new loadoutID.
/// </summary>
public readonly struct LoadoutMarker
{
    private readonly LoadoutRegistry _registry;
    private readonly LoadoutId _id;

    /// <summary/>
    /// <param name="registry"></param>
    /// <param name="id"></param>
    public LoadoutMarker(LoadoutRegistry registry, LoadoutId id)
    {
        _registry = registry;
        _id = id;
    }

    /// <summary>
    /// Gets the state of the loadout represented by the current ID.
    /// </summary>
    public Loadout Value => _registry.Get(_id)!;

    /// <summary>
    /// Returns all of the previous versions of this loadout for.
    /// </summary>
    public IEnumerable<Loadout> History()
    {
        var list = Value;
        // This exists to deal with bad data we may have for previous list versions
        // for example if we've purged the previous versions of a list
        while (list is not null)
        {
            yield return list;
            if (list.PreviousVersion.Id.Equals(IdEmpty.Empty))
                break;

            list = list.PreviousVersion.Value;
        }
    }

    /// <summary>
    /// Adds a known mod to the given loadout.
    /// </summary>
    /// <param name="newMod">The mod to add to the loadout.</param>
    public void Add(Mod newMod)
    {
        _registry.Alter(_id, $"Added mod: {newMod.Name}", l => l.Add(newMod));
    }

    /// <summary>
    /// Removes a known mod from the given loadout.
    /// </summary>
    /// <param name="oldMod">The mod to remove from the loadout.</param>
    public void Remove(Mod oldMod)
    {
        _registry.Alter(_id, $"Remove mod: {oldMod.Name}", l => l.Remove(oldMod));
    }

    /// <summary>
    /// Alter the loadout.
    /// </summary>
    /// <param name="changeMessage"></param>
    /// <param name="func"></param>
    public void Alter(string changeMessage, Func<Loadout, Loadout> func)
    {
        _registry.Alter(_id, changeMessage, func);
    }
}
