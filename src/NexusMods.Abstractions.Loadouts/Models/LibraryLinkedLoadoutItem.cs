using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Library;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a loadout item group that is linked to a library item.
/// </summary>
/// <remarks>
/// This is only created by the installer and shouldn't be created manually.
/// </remarks>
[Include<LoadoutItemGroup>]
[PublicAPI]
public partial class LibraryLinkedLoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LibraryLinkedLoadoutItem";

    /// <summary>
    /// The linked library item.
    /// </summary>
    public static readonly ReferenceAttribute<LibraryItem> LibraryItem = new(Namespace, nameof(LibraryItem)) { IsIndexed = true };

    public readonly partial struct ReadOnly
    {
        /// <summary>
        /// Tries converting this entity to a <see cref="LoadoutItem"/> entity,
        /// if the entity is not a <see cref="LoadoutItem"/> entity, it returns false.
        /// </summary>
        public LoadoutItem.ReadOnly AsLoadoutItem() => new(Db, Id);
    }
}
