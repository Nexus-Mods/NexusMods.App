
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A loadout can be thought of as a mod list that is specific to a certain
/// installation of a game.
/// </summary>
public static class Loadout
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Loadout";

    /// <summary>
    /// The human friendly name for this loadout.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// Unique installation of the game this loadout is tied to.
    /// </summary>
    public static readonly ReferenceAttribute Installation = new(Namespace, nameof(Installation));
    
    /// <summary>
    /// A revision number for this loadout. Each change to a file/mod in the loadout should increment
    /// this value by one. This will then be used by the UI and other parts of the app to determine
    /// what aspects of the loadout have changed and need to be reloaded
    /// </summary>
    public static readonly ULongAttribute Revision = new(Namespace, nameof(Revision));
    
    /// <summary>
    /// Defines the 'type' of layout that this layout represents.
    /// Currently it is just `Default`, 'Deleted' and `VanillaState` type, with
    /// `marker` being a special hidden loadout type that represents
    /// a game's base state as it was added to the App.
    /// </summary>
    public static readonly EnumByteAttribute<LoadoutKind> LoadoutKind = new(Namespace, nameof(LoadoutKind));
    
    /// <summary>
    /// Retrieves all loadouts from the database.
    /// </summary>
    public static IEnumerable<Model> Loadouts(this IDb db)
    {
        return db.Find(Name)
            .Select(db.Get<Model>);
    }

    /// <summary>
    /// Finds one or more loadouts by name.
    /// </summary>
    public static IEnumerable<Model> ByName(IDb db, string name)
    {
        return db.FindIndexed(name, Name)
            .Select(db.Get<Model>);
    }

    /// <summary>
    /// Gets all the revisions of a loadout over time
    /// </summary>
    public static IObservable<Model> Revisions(this IConnection conn, LoadoutId id)
    {
        // All db revisions that contain the loadout id, select the loadout
        return conn.Revisions
            .Where(db => db.Datoms(db.BasisTxId).Any(datom => datom.E == id.Value))
            .StartWith(conn.Db)
            .Select(db => db.Get<Model>(id.Value));
    }

    /// <summary>
    /// Retracts a loadout, performing an effective deletion.
    /// </summary>
    /// <param name="conn">The connection to use.</param>
    /// <param name="loadoutId">The ID of the loadout which needs to be retracted.</param>
    public static async Task Delete(this IConnection conn, LoadoutId loadoutId)
    {
        using var db = conn.Db;
        var loadout = db.Get<Model>(loadoutId.Value);

        using var tx = conn.BeginTransaction();

        // Retract the loadout itself by changing its kind to `Deleted`
        // This marks the entity for Garbage Collection on next GC run.
        LoadoutKind.Add(tx, loadout.Id, Abstractions.Loadouts.LoadoutKind.Deleted, false);
        loadout.Revise(tx);
        await tx.Commit();
    }
    
    public class Model(ITransaction tx) : Entity(tx)
    {
        /// <summary>
        /// The unique identifier for this loadout, casted to a <see cref="LoadoutId"/>.
        /// </summary>
        public LoadoutId LoadoutId => LoadoutId.From(Id);

        /// <summary>
        /// Gets the loadout id/txid pair for this revision of the loadout.
        /// </summary>
        public LoadoutWithTxId GetLoadoutWithTxId()
        {
            return new LoadoutWithTxId(LoadoutId, GetRevisionTxId());
        }
        
        /// <summary>
        /// The most recent transaction Id that modified this loadout model
        /// </summary>
        public TxId GetRevisionTxId() => Db.Datoms(Id).Max(d => d.T);

        public string Name
        {
            get => Loadout.Name.Get(this);
            set => Loadout.Name.Add(this, value);
        }
        
        /// <summary>
        /// Get the installation id for this loadout.
        /// </summary>
        public EntityId InstallationId
        {
            get => Loadout.Installation.Get(this);
            set => Loadout.Installation.Add(this, value);
        }

        /// <summary>
        /// The revision number for this loadout, increments by one for each change.
        /// </summary>
        public ulong Revision
        {
            get => Loadout.Revision.Get(this, 0);
            set => Loadout.Revision.Add(this, value);
        }

        /// <summary>
        /// Get the game installation for this loadout.
        /// </summary>
        public GameInstallation Installation
        {
            get
            {
                var registry = ServiceProvider.GetRequiredService<IGameRegistry>();
                if (!registry.Installations.TryGetValue(Loadout.Installation.Get(this), out var found))
                    throw new KeyNotFoundException("Game installation not found in registry");
                return found;
            }
            set => Loadout.Installation.Add(this, value.GameMetadataId);
        }

        /// <summary>
        /// Gets all mods in this loadout.
        /// </summary>
        public Entities<EntityIds, Mod.Model> Mods => GetReverse<Mod.Model>(Mod.Loadout);
        
        /// <summary>
        /// Gets all the files in this loadout.
        /// </summary>
        public Entities<EntityIds, File.Model> Files => GetReverse<File.Model>(File.Loadout);

        /// <summary>
        /// Specifies the type of the loadout that the current loadout represents
        /// </summary>
        public LoadoutKind LoadoutKind 
        {
            get => Loadout.LoadoutKind.Get(this, 0);
            set => Loadout.LoadoutKind.Add(this, value);
        }
                
        /// <summary>
        /// Gets the mod with the given id from this loadout.
        /// </summary>
        public Mod.Model this[ModId idx]
        {
            get
            {
                var mod = Db.Get<Mod.Model>(idx.Value);
                if (mod is null) 
                    throw new KeyNotFoundException($"Mod with id {idx} not found in database");
                if (mod.LoadoutId != LoadoutId)
                    throw new KeyNotFoundException($"Mod with id {idx} is not part of Loadout {LoadoutId} but of Loadout {mod.LoadoutId}");
                return mod;
            }
        }

        /// <summary>
        /// Issue a new revision of this loadout into the transaction, this will increment the revision number
        /// </summary>
        public void Revise(ITransaction tx)
        {
            tx.Add(Id, static (innerTx, db, id) =>
            {
                var self = db.Get<Model>(id);
                innerTx.Add(id, Loadout.Revision, self.Revision + 1);
            });
        }
        
        /// <summary>
        /// This is true if the loadout is the 'Vanilla State' loadout.
        /// This loadout is created from the original game state and should
        /// be a singleton for a given game. It should never be mutated outside
        /// of ingesting game updates.
        ///
        /// These loadouts should not be shown in any user facing elements.
        /// </summary>
        public bool IsVanillaStateLoadout() => LoadoutKind == LoadoutKind.VanillaState;

        /// <summary>
        /// Returns true if the loadout should be visible to the user.
        /// A non-visible loadout should be treated as if it doesn't exist.
        /// </summary>
        /// <remarks>
        /// Note(sewer), it's better to 'opt into' functionality, than opt out.
        /// especially, when it comes to displaying elements the user can edit.
        /// </remarks>
        public bool IsVisible() => LoadoutKind == LoadoutKind.Default;
    }
}
