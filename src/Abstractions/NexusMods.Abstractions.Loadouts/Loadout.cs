
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
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
    }

}
