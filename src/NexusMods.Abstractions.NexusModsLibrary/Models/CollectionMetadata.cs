using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Resources;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Metadata about a collection on Nexus Mods.
/// </summary>
public partial class CollectionMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionMetadata";

    /// <summary>
    /// The collection ID.
    /// </summary>
    public static readonly CollectionIdAttribute CollectionId = new(Namespace, nameof(CollectionId)) { IsIndexed = true, IsUnique = true };

    /// <summary>
    /// The collection slug.
    /// </summary>
    public static readonly CollectionsSlugAttribute Slug = new(Namespace, nameof(Slug)) { IsIndexed = true };

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The id of the game.
    /// </summary>
    public static readonly GameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };

    /// <summary>
    /// The short description of the collection
    /// </summary>
    public static readonly StringAttribute Summary = new(Namespace, nameof(Summary)) { IsOptional = true };

    /// <summary>
    /// The Curating user of the collection.
    /// </summary>
    public static readonly ReferenceAttribute<User> Author = new(Namespace, nameof(Author));
    
    /// <summary>
    /// The revisions of the collection.
    /// </summary>
    public static readonly BackReferenceAttribute<CollectionRevisionMetadata> Revisions = new(CollectionRevisionMetadata.Collection);
    
    /// <summary>
    /// The category of the collection.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionCategory> Category = new(Namespace, nameof(Category)) { IsOptional = true };

    /// <summary>
    /// Total number of times the collection was downloaded.
    /// </summary>
    public static readonly UInt64Attribute TotalDownloads = new(Namespace, nameof(TotalDownloads)) { IsOptional = true };

    /// <summary>
    /// The number of endorsements the collection has.
    /// </summary>
    public static readonly UInt64Attribute Endorsements = new(Namespace, nameof(Endorsements)) { IsOptional = true };

    /// <summary>
    /// The tile image uri.
    /// </summary>
    public static readonly UriAttribute TileImageUri = new(Namespace, nameof(TileImageUri)) { IsOptional = true };

    /// <summary>
    /// The background image uri.
    /// </summary>
    public static readonly UriAttribute BackgroundImageUri = new(Namespace, nameof(BackgroundImageUri)) { IsOptional = true };

    /// <summary>
    /// The tile image resource.
    /// </summary>
    public static readonly ReferenceAttribute<PersistedDbResource> TileImageResource = new(Namespace, nameof(TileImageResource)) { IsOptional = true };

    /// <summary>
    /// The background image resource.
    /// </summary>
    public static readonly ReferenceAttribute<PersistedDbResource> BackgroundImageResource = new(Namespace, nameof(BackgroundImageResource)) { IsOptional = true };

    /// <summary>
    /// An average taken from all revision ratings.
    /// </summary>
    public static readonly Float32Attribute OverallRating = new(Namespace, nameof(OverallRating)) { IsOptional = true };

    /// <summary>
    /// Total number of ratings given across all revisions
    /// </summary>
    public static readonly Int32Attribute OverallRatingCount = new(Namespace, nameof(OverallRatingCount)) { IsOptional = true };

    /// <summary>
    /// A 30-day average of all revision ratings.
    /// </summary>
    public static readonly Float32Attribute RecentRating = new(Namespace, nameof(RecentRating)) { IsOptional = true };

    /// <summary>
    /// Total number of ratings given in the last 30 days.
    /// </summary>
    public static readonly Int32Attribute RecentRatingCount = new(Namespace, nameof(RecentRatingCount)) { IsOptional = true };

    /// <summary>
    /// Listing status.
    /// </summary>
    public static readonly EnumAttribute<CollectionStatus> Status = new(Namespace, nameof(Status)) { IsOptional = true };
}
