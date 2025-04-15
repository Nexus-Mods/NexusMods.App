using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Telemetry;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Metadata about a collection revision on Nexus Mods. A revision references a collection, but is itself immutable.
/// Each change to a collection is expressed as a separate revision.
/// </summary>
public partial class CollectionRevisionMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionRevisionMetadata";
    
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
    public static readonly BackReferenceAttribute<CollectionDownload> Downloads = new(CollectionDownload.CollectionRevision);

    /// <summary>
    /// Whether the collection contains adult mods.
    /// </summary>
    public static readonly BooleanAttribute IsAdult = new(Namespace, nameof(IsAdult));

    /// <summary>
    /// Total download size according to external sources.
    /// </summary>
    public static readonly SizeAttribute TotalSize = new(Namespace, nameof(TotalSize));

    /// <summary>
    /// The overall rating of this revision (often displayed as a percentage).
    /// </summary>
    public static readonly FloatAttribute OverallRating = new(Namespace, nameof(OverallRating)) { IsOptional = true };

    /// <summary>
    /// The total number of ratings this revision has.
    /// </summary>
    public static readonly UInt64Attribute TotalRatings = new(Namespace, nameof(TotalRatings)) { IsOptional = true };
}
