using DynamicData.Kernel;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A definition of a collection. This could be a user's collection, a Nexus Mods collection or perhaps a collection from some other source.
/// </summary>
[Include<LoadoutItemGroup>]
public partial class CollectionGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.CollectionGroup";
    
    /// <summary>
    /// Mostly a marker attribute to find all the collections on a given loadout. Likely faster than searching for all the loadout items
    /// and filtering them by their attributes
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));

    /// <summary>
    /// In this UI this is reflected as "My Collection", it's a group of all mods that the user has installed outside of any
    /// other collection.
    /// </summary>
    public static readonly MarkerAttribute UserCollection = new(Namespace, nameof(UserCollection));
}

public static partial class CollectionGroupLoaderExtensions
{
    /// <summary>
    /// Find the user collection for a given loadout
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public static Optional<CollectionGroup.ReadOnly> FindUserCollection(this Loadout.ReadOnly loadout)
    {
        return loadout.Db
            .Datoms(CollectionGroup.Loadout, loadout.Id)
            .Select(d => CollectionGroup.Load(loadout.Db, d.E))
            .FirstOrOptional(x => x.IsUserCollection);
    }
}
