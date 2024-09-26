using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Metadata about a collection revision on Nexus Mods. A revision references a collection, but is itself immutable.
/// Each change to a collection is expressed as a separate revision.
/// </summary>
public partial class CollectionRevisionMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NexusModsCollectionRevision";
    
    /// <summary>
    /// The globally unique id identifying a specific revision of a collection.
    /// </summary>
    public static readonly RevisionIdAttribute RevisionId = new(Namespace, nameof(RevisionId)) { IsIndexed = true };
    
    /// <summary>
    /// The locally unique revision number (aka "version") of a collection. Only unique within one collection.
    /// </summary>
    public static readonly RevisionNumberAttribute RevisionNumber = new(Namespace, nameof(RevisionNumber)) { IsIndexed = true };
    
    /// <summary>
    /// The collection this revision belongs to.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionMetadata> Collection = new(Namespace, nameof(Collection));
    
    /// <summary>
    /// All the mod files in this revision.
    /// </summary>
    public static readonly BackReferenceAttribute<CollectionRevisionModFile> Files = new(CollectionRevisionModFile.CollectionRevision);
    
    /// <summary>
    /// The number of downloads this revision has.
    /// </summary>
    public static readonly ULongAttribute Downloads = new(Namespace, nameof(Downloads));
    
    /// <summary>
    /// Total download size of all files in this revision, including the size of the revision's files.
    /// </summary>
    public static readonly SizeAttribute TotalSize = new(Namespace, nameof(TotalSize));
    
    /// <summary>
    /// The overall rating of this revision (often displayed as a percentage).
    /// </summary>
    public static readonly FloatAttribute OverallRating = new(Namespace, nameof(OverallRating));
    
    /// <summary>
    /// The total number of ratings this revision has.
    /// </summary>
    public static readonly ULongAttribute TotalRatings = new(Namespace, nameof(TotalRatings));
}
