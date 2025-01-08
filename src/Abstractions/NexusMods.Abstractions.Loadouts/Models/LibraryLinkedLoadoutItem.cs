using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

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
}
