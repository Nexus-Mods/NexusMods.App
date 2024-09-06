using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

[Include<LoadoutItemGroup>]
public partial class NexusCollectionLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Collections.NexusCollectionLoadoutItem";
    
    /// <summary>
    /// The collection library file.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsCollectionLibraryFile> LibraryFile = new(Namespace, nameof(LibraryFile));
}
