using DynamicData;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts.Visitors;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Games.Loadouts;

/// <summary>
/// Provides the main entry point for listing, modifying and updating loadouts.
/// </summary>
public interface ILoadoutRegistry
{
    /// <summary>
    /// All the loadoutIds and their current root entity IDs
    /// </summary>
    IObservable<IChangeSet<IId, LoadoutId>> LoadoutChanges { get; }

    /// <summary>
    /// All the loadouts and their current root ids
    /// </summary>
    IObservable<IChangeSet<Loadout, LoadoutId>> Loadouts { get; }

    /// <summary>
    /// All games that have loadouts
    /// </summary>
    IObservable<IDistinctChangeSet<IGame>> Games { get; }

    /// <summary>
    /// Alters the loadout with the given id. If the loadout does not exist, it is created.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterFn"></param>
    /// <returns></returns>
    Loadout Alter(LoadoutId id, string commitMessage, Func<Loadout, Loadout> alterFn);

    /// <summary>
    /// Alters the mod with the given id in the loadout with the given id. If the alter
    /// function returns null, the mod is removed from the loadout.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterfn"></param>
    void Alter(LoadoutId loadoutId, ModId modId, string commitMessage, Func<Mod?, Mod?> alterfn);

    /// <summary>
    /// Alters the file with the given id in the mod with the given id in the loadout with the given id. If the file
    /// does not exist, an error is thrown.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <param name="fileId"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterFn"></param>
    /// <typeparam name="T"></typeparam>
    void Alter<T>(LoadoutId loadoutId, ModId modId, ModFileId fileId, string commitMessage, Func<T, T> alterFn)
        where T : AModFile;

    /// <summary>
    /// Alters the mod pointed to by the cursor. If the alter function returns null, the mod is removed from the loadout.
    /// </summary>
    /// <param name="cursor"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterFn"></param>
    void Alter(ModCursor cursor, string commitMessage, Func<Mod?, Mod?> alterFn);

    /// <summary>
    /// Modify the loadout with the given id using the given visitor. This is not very
    /// optimized, so should only be used in situations were large scale transformations
    /// are being done. The methods on the visitor will be called for every part of the
    /// loadout.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commitMessage"></param>
    /// <param name="visitor"></param>
    Loadout Alter(LoadoutId id, string commitMessage, ALoadoutVisitor visitor);

    /// <summary>
    /// Gets the id of the loadout with the given loadout id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    IId? GetId(LoadoutId id);

    /// <summary>
    /// Gets the loadout with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    Loadout? Get(LoadoutId id);

    /// <summary>
    /// Gets the mod pointed to by the cursor.
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    Mod? Get(ModCursor cursor);

    /// <summary>
    /// Gets the mod with the given id from the given loadout.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <returns></returns>
    Mod? Get(LoadoutId loadoutId, ModId modId);

    /// <summary>
    /// Loads the loadout with the given id, or null if it does not exist.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Loadout? GetLoadout(IId id);

    /// <summary>
    /// Finds the loadout with the given name (case insensitive).
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IEnumerable<Loadout> GetByName(string name);

    /// <summary>
    /// Returns all loadout ids.
    /// </summary>
    /// <returns></returns>
    IEnumerable<LoadoutId> AllLoadoutIds();

    /// <summary>
    /// Returns all loadouts.
    /// </summary>
    /// <returns></returns>
    IEnumerable<Loadout> AllLoadouts();

    /// <summary>
    /// An observable of all the revisions of a given LoadoutId
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    IObservable<IId> Revisions(LoadoutId loadoutId);

    /// <summary>
    /// An observable of all the revisions of a given loadout and mod
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <returns></returns>
    IObservable<IId> Revisions(LoadoutId loadoutId, ModId modId);

    /// <summary>
    /// Gets the revisions of a given cursor
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    IObservable<IId> Revisions(ModCursor cursor);

    /// <summary>
    /// Same as Revisions, but returns the loadouts instead of the ids.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    IObservable<Loadout> RevisionsAsLoadouts(LoadoutId loadoutId);

    /// <summary>
    /// Returns the current and future revisions for a mod pointed
    /// to by the cursor. Same as Revisions, but returns the mods
    /// instead of the ids.
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    IObservable<Mod> RevisionsAsMods(ModCursor cursor);

    /// <inheritdoc/>
    void Dispose();

    /// <summary>
    /// Gets the marker for the given loadout id.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    LoadoutMarker GetMarker(LoadoutId loadoutId);

    /// <summary>
    /// Suggestions a name for a new loadout.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    string SuggestName(GameInstallation installation);

    /// <summary>
    /// Manages the given installation, returning a marker for the new loadout.
    /// </summary>
    /// <param name="installation"></param>
    /// <param name="name">Name for the new loadout.</param>
    /// <returns></returns>
    Task<LoadoutMarker> Manage(GameInstallation installation, string name = "");

    /// <summary>
    /// Returns true if the given loadout id exists.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    bool Contains(LoadoutId loadoutId);
}
