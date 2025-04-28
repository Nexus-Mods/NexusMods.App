using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Cascade;
using NexusMods.Cascade.Rules;
using NexusMods.Cascade.Structures;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represented a <see cref="LibraryItem"/> originating from Nexus Mods.
/// </summary>
[PublicAPI]
[Include<LibraryItem>]
public partial class NexusModsLibraryItem : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.NexusModsLibraryItem";

    /// <summary>
    /// Remote metadata of the file on Nexus Mods.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsFileMetadata> FileMetadata = new(Namespace, nameof(FileMetadata));

    /// <summary>
    /// Remote metadata of the mod page on Nexus Mods.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsModPageMetadata> ModPageMetadata = new(Namespace, nameof(ModPageMetadata));

    public static readonly Flow<(string Name, EntityId ModPageId, int LoadoutItemCount)> LoadoutItemCounts =
        Pattern.Create()
            .Db(out var loadoutItemId, LibraryLinkedLoadoutItem.LibraryItemId, out var libraryItemId)
            .Db(libraryItemId, NexusModsLibraryItem.ModPageMetadata, out var modPageId)
            .Db(modPageId, NexusModsModPageMetadata.Name, out var modPageName)
            .Return(modPageName, modPageId, loadoutItemId.Count());
    
}
