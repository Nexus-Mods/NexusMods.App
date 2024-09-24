using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// A library file that is a Nexus Mods collection download.
/// </summary>
[PublicAPI]
[Include<LibraryFile>]
public partial class NexusModsCollectionLibraryFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.NexusModsLibrary.Models.NexusModsCollectionLibraryFile";
    
    /// <summary>
    /// The collection revision this file belongs to.
    /// </summary>
    public static readonly ReferenceAttribute<Models.CollectionRevisionMetadata> CollectionRevision = new(Namespace, nameof(CollectionRevision));
}
