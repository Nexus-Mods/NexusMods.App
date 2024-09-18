using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Metadata about a collection on Nexus Mods.
/// </summary>
public partial class CollectionRevision : IModelDefinition
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
    public static readonly ReferenceAttribute<Collection> Collection = new(Namespace, nameof(Collection));
    
    /// <summary>
    /// The number of downloads this revision has.
    /// </summary>
    public static readonly ULongAttribute Downloads = new(Namespace, nameof(Downloads));
    
    /// <summary>
    /// Total size of the revision
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
    
    /// <summary>
    /// The total number of mods in this revision.
    /// </summary>
    public static readonly ULongAttribute ModCount = new(Namespace, nameof(ModCount));
}
