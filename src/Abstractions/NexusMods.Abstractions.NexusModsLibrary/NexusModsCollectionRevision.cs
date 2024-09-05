using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Metadata about a collection on Nexus Mods.
/// </summary>
public partial class NexusModsCollectionRevision : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NexusModsCollectionRevision";
    
    /// <summary>
    /// The globally unique id identifying a specific revision of a collection.
    /// </summary>
    public static readonly RevisionIdAttribute RevisionId = new(Namespace, nameof(RevisionId)) { IsIndexed = true };
    
    /// <summary>
    /// The locally unique revision number (aka "version") of a collection. Only unique within one collection.
    /// </summary>
    public static readonly RevisionNumberAttribute RevisionNumber = new(Namespace, nameof(RevisionNumber));
    
    /// <summary>
    /// The collection this revision belongs to.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsCollectionMetadata> Collection = new(Namespace, nameof(Collection));
}
