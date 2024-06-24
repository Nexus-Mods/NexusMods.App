
using System.Reactive.Linq;
using DynamicData.Cache.Internal;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
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
public partial class Loadout : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Loadout";

    /// <summary>
    /// The human friendly name for this loadout.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// Unique installation of the game this loadout is tied to.
    /// </summary>
    public static readonly ReferenceAttribute<GameMetadata> Installation = new(Namespace, nameof(Installation));
    
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
    /// Mods that are part of this loadout point to this entity via Mod.Loadout
    /// </summary>
    public static readonly BackReferenceAttribute<Mod> Mods = new(Mod.Loadout);
    
    /// <summary>
    /// All the files that are part of this loadout point to this entity via File.Loadout
    /// </summary>
    public static readonly BackReferenceAttribute<File> Files = new(File.Loadout);
    
    public partial struct ReadOnly
    {
        /// <summary>
        /// Get the game installation for this loadout.
        /// </summary>
        public GameInstallation InstallationInstance
        {
            get
            {
                var registry = Db.Connection.ServiceProvider.GetRequiredService<IGameRegistry>();
                if (!registry.Installations.TryGetValue(Loadout.Installation.Get(this), out var gameInstallation))
                    throw new KeySelectorException($"Game installation of `{Installation.Domain}` at `{Installation.Path}` not found in registry!");
                return gameInstallation;
            }
        }
        
        /// <summary>
        /// Issue a new revision of this loadout into the transaction, this will increment the revision number
        /// </summary>
        public void Revise(ITransaction tx)
        {
            tx.Add(Id, static (innerTx, db, id) =>
            {
                var self = ReadOnly.Create(db, id);
                innerTx.Add(id, Loadout.Revision, self.Revision + 1);
            });
        }
        
        /// <summary>
        /// Get the loadout tx pair for this loadout.
        /// </summary>
        public LoadoutWithTxId GetLoadoutWithTxId()
        {
            return new(Id, this.Max(d => d.T));
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


public partial struct LoadoutId
{
    
    /// <summary>
    /// Try to parse a LoadoutId from a hex string.
    /// </summary>
    public static bool TryParseFromHex(string hex, out LoadoutId id)
    {
        
        if (EntityExtensions.TryParseFromHex(hex, out var entityId))
        {
            id = From(entityId);
            return true;
        }
        id = default(LoadoutId);
        return false;
    }
}
