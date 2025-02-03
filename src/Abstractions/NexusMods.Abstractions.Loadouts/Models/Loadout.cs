using DynamicData.Cache.Internal;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A loadout can be thought of as a mod list that is specific to a certain
/// installation of a game.
/// </summary>
public partial class Loadout : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.Loadout";

    /// <summary>
    /// The human friendly name for this loadout.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// A capital one-letter name for the loadout, for spine labels and other places where space is limited.
    /// </summary>
    public static readonly StringAttribute ShortName = new(Namespace, nameof(ShortName)) { IsIndexed = true, DefaultValue = "-"};
    
    /// <summary>
    /// The locator ids for the game this loadout is tied to, for gog this will be the build ids, for example. 
    /// </summary>
    public static readonly StringsAttribute LocatorIds = new(Namespace, nameof(LocatorIds));
    
    /// <summary>
    /// The game version that this loadout is tied to, based on the LocatorIds.
    /// </summary>
    public static readonly StringAttribute GameVersion = new(Namespace, nameof(GameVersion));
    
    /// <summary>
    /// Unique installation of the game this loadout is tied to.
    /// </summary>
    public static readonly ReferenceAttribute<GameInstallMetadata> Installation = new(Namespace, nameof(Installation));
    
    /// <summary>
    /// A revision number for this loadout. Each change to a file/mod in the loadout should increment
    /// this value by one. This will then be used by the UI and other parts of the app to determine
    /// what aspects of the loadout have changed and need to be reloaded
    /// </summary>
    public static readonly UInt64Attribute Revision = new(Namespace, nameof(Revision));

    /// <summary>
    /// Defines the 'type' of layout that this layout represents.
    /// Currently it is just `Default`, 'Deleted' and `VanillaState` type, with
    /// `marker` being a special hidden loadout type that represents
    /// a game's base state as it was added to the App.
    /// </summary>
    public static readonly EnumByteAttribute<LoadoutKind> LoadoutKind = new(Namespace, nameof(LoadoutKind));

    /// <summary>
    /// DateTime when the loadout was last applied.
    /// Returns DateTime.MinValue if the loadout has never been applied.
    /// </summary>
    public static readonly TimestampAttribute LastAppliedDateTime = new(Namespace, nameof(LastAppliedDateTime))
    {
        IsOptional = true,
        DefaultValue = DateTimeOffset.MinValue,
    };

    /// <summary>
    /// All items in the Loadout.
    /// </summary>
    public static readonly BackReferenceAttribute<LoadoutItem> Items = new(LoadoutItem.Loadout);

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
                    throw new KeySelectorException($"Game installation of `{Installation.GameId}` at `{Installation.Path}` not found in registry!");
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
            return new LoadoutWithTxId(Id, this.Max(d => d.T));
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

        /// <summary>
        /// Returns an enumerable containing all loadout items linked to the given library item.
        /// </summary>
        public IEnumerable<LibraryLinkedLoadoutItem.ReadOnly> GetLoadoutItemsByLibraryItem(LibraryItem.ReadOnly libraryItem)
        {
            var thisId = LoadoutId; // Compiler complains about using `this` in a lambda otherwise
            
            // Start with a backref. This assumes that the number of loadouts with a given library item will be fairly small.
            // This could be false, but it's a good starting point.
            return LibraryLinkedLoadoutItem
                .FindByLibraryItem(Db, libraryItem)
                .Where(linked => linked.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == thisId);
        }
    }
}
