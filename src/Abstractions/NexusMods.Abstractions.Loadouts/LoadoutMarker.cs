using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a mutable marker of a loadout. Operations on this class
/// may mutate a loadout, which will then cause a "rebase" of this marker
/// on the new loadoutID.
/// </summary>
public class LoadoutMarker
{
    private readonly ILoadoutRegistry _registry;
    private IId _dataStoreId = IdEmpty.Empty;
    private Guid _uniqueId = Guid.NewGuid();

    /// <summary/>
    /// <param name="registry"></param>
    /// <param name="id"></param>
    public LoadoutMarker(ILoadoutRegistry registry, LoadoutId id)
    {
        _registry = registry;
        _dataStoreId = _registry.Get(id)!.DataStoreId;
    }

    /// <summary>
    /// Converts the marker to a string representation by its name
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Value.Name;
    }

    /// <summary>
    /// Sets the current data store ID of the loadout.
    /// </summary>
    /// <remarks>Use with caution. This is more of an internal knob.</remarks>
    public void SetDataStoreId(IId id) => _dataStoreId = id;

    /// <summary>
    /// Gets the current data store ID of the loadout.
    /// </summary>
    public IId DataStoreId => _dataStoreId;

    /// <summary>
    /// Gets the state of the loadout represented by the current ID.
    /// </summary>
    public Loadout Value => _registry.GetLoadout(_dataStoreId)!;

    /// <summary>
    /// Returns the ID of the loadout.
    /// </summary>
    public LoadoutId Id => Value.LoadoutId;

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
        _registry.Alter(Id, $"Added mod: {newMod.Name}", l => l.Add(newMod));
    }

    /// <summary>
    /// Removes a known mod from the given loadout.
    /// </summary>
    /// <param name="oldMod">The mod to remove from the loadout.</param>
    public void Remove(Mod oldMod)
    {
        _registry.Alter(Id, $"Remove mod: {oldMod.Name}", l => l.Remove(oldMod));
    }

    /// <summary>
    /// Alter the loadout.
    /// </summary>
    /// <param name="changeMessage"></param>
    /// <param name="func"></param>
    public void Alter(string changeMessage, Func<Loadout, Loadout> func)
    {
        _registry.Alter(Id, changeMessage, func);
    }
}
