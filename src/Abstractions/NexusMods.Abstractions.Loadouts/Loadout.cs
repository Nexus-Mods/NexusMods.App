using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A loadout can be thought of as a mod list that is specific to a certain
/// installation of a game.
/// </summary>
/// <remarks>
///    We treat loadouts kind of like git branches, the document <a href="https://github.com/Nexus-Mods/NexusMods.App/blob/main/docs/ImmutableModlists.md">Immutable Mod Lists</a>
///    might provide you with some additional insight into the idea.
/// </remarks>
[JsonName("NexusMods.Abstractions.Games.Loadouts.Loadout")]
public record Loadout : Entity, IEmptyWithDataStore<Loadout>
{
    /// <summary>
    /// Collection of mods.
    /// </summary>
    public required EntityDictionary<ModId, Mod> Mods { get; init; }

    /// <summary>
    /// Unique identifier for this loadout in question.
    /// </summary>
    public required LoadoutId LoadoutId { get; init; }

    /// <summary>
    /// Human friendly name for this loadout.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Unique installation of the game this loadout is tied to.
    /// </summary>
    public required GameInstallation Installation { get; init; }

    /// <summary>
    /// The time this loadout is last modified.
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Link to the previous version of this loadout on the data store.
    /// </summary>
    public required EntityLink<Loadout> PreviousVersion { get; init; }
    
    /// <summary>
    /// This is true if the loadout is a hidden 'Marker' loadout.
    /// A marker loadout is created from the original game state and should
    /// be a singleton for a given game. It is a temporary loadout that is
    /// destroyed when a real loadout is applied.
    ///
    /// Marker loadouts should not be shown in any user facing elements.
    /// </summary>
    public bool IsMarkerLoadout { get; init; }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Loadouts;

    /// <summary>
    /// Summarises the changes made in this version of the loadout.
    /// Think of this like a git commit message.
    /// </summary>
    public required string ChangeMessage { get; init; } = "";

    /// <inheritdoc />
    public static Loadout Empty(IDataStore store) => new()
    {
        LoadoutId = LoadoutId.Create(),
        Installation = GameInstallation.Empty,
        Name = "",
        Mods = EntityDictionary<ModId, Mod>.Empty(store),
        LastModified = DateTime.UtcNow,
        PreviousVersion = EntityLink<Loadout>.Empty(store),
        ChangeMessage = ""
    };

    /// <summary>
    /// Makes a change to the collection of mods stored.
    /// </summary>
    /// <param name="modId">Unique identifier for this mod.</param>
    /// <param name="func">Function used to change the details of this mod.</param>
    /// <returns>A new loadout with the details of a single mod changed.</returns>
    public Loadout Alter(ModId modId, Func<Mod, Mod?> func)
    {
        return this with
        {
            Mods = Mods.Keep(modId, func)
        };
    }

    /// <summary>
    /// Adds an individual mod to this loadout, returning a new loadout.
    /// </summary>
    /// <param name="mod">An individual modification to add to this loadout.</param>
    /// <returns>The loadout with this modification added.</returns>
    public Loadout Add(Mod mod)
    {
        return this with
        {
            Mods = Mods.With(mod.Id, mod)
        };
    }

    /// <summary>
    /// Remove an individual mod from this loadout, returning a new loadout.
    /// </summary>
    /// <param name="mod"></param>
    /// <returns></returns>
    public Loadout Remove(Mod mod)
    {
        return this with
        {
            Mods = Mods.Without(mod.Id)
        };
    }

    /// <summary>
    /// Allows you to change individual files associated with a mod in this collection.
    /// </summary>
    /// <param name="func">Function used to change the files of a given mod.</param>
    /// <returns>A new loadout with the files of mods changed.</returns>
    public Loadout AlterFiles(Func<AModFile, AModFile?> func)
    {
        return this with
        {
            Mods = Mods.Keep(m => m with
            {
                Files = m.Files.Keep(func)
            })
        };
    }
}
