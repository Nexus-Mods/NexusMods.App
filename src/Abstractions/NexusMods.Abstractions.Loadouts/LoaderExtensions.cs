using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Extensions to IDb/IConnection for loading loadouts and other entities
/// </summary>
public static class LoaderExtensions
{
    /// <summary>
    /// Get a loadout by id
    /// </summary>
    public static Loadout.Model Get(this IDb db, LoadoutId id) => db.Get<Loadout.Model>(id.Value);
    
    /// <summary>
    /// Gets all loadouts with a given name
    /// </summary>
    public static IEnumerable<Loadout.Model> GetByName(this IDb db, string name) => 
        db.FindIndexed(name, Loadout.Name)
            .Select(db.Get<Loadout.Model>);
    
    /// <summary>
    /// Get a mod by id
    /// </summary>
    public static Mod.Model Get(this IDb db, ModId id) => db.Get<Mod.Model>(id.Value);
}
