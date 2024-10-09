using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Collections;

/// <summary>
/// A mod that was bundled with a collection
/// </summary>
[Include<LoadoutItemGroup>]
public partial class NexusCollectionBundledLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.NexusCollectionBundledLoadoutGroup";
    
    /// <summary>
    /// The downloaded collection archive that this mod was bundled with
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsCollectionLibraryFile> CollectionLibraryFile = new(Namespace, nameof(CollectionLibraryFile));
}
