
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
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
    /// Retrieves all loadouts from the database.
    /// </summary>
    public static IEnumerable<Model> All(IDb db)
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
    
    public class Model(ITransaction tx) : Entity(tx)
    {
        /// <summary>
        /// The unique identifier for this loadout, casted to a <see cref="LoadoutId"/>.
        /// </summary>
        public LoadoutId LoadoutId => LoadoutId.From(Id);
        
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
            get => ServiceProvider.GetRequiredService<IGameRegistry>()
                .Get(Loadout.Installation.Get(this));
            set => Loadout.Installation.Add(this, value.Id);
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
    }

}
