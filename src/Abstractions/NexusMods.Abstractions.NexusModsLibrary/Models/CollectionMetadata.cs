using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.Abstractions.Resources.DB;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Metadata about a collection on Nexus Mods.
/// </summary>
public partial class CollectionMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NexusModsCollectionMetadata";
    
    /// <summary>
    /// The collection slug.
    /// </summary>
    public static readonly CollectionsSlugAttribute Slug = new(Namespace, nameof(Slug)) { IsIndexed = true };
    
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// The short description of the collection
    /// </summary>
    public static readonly StringAttribute Summary = new(Namespace, nameof(Summary));
    
    /// <summary>
    /// The Curating user of the collection.
    /// </summary>
    public static readonly ReferenceAttribute<User> Author = new(Namespace, nameof(Author));
    
    /// <summary>
    /// The revisions of the collection.
    /// </summary>
    public static readonly BackReferenceAttribute<CollectionRevisionMetadata> Revisions = new(CollectionRevisionMetadata.Collection);
    
    /// <summary>
    /// The tags on the collection.
    /// </summary>
    public static readonly ReferencesAttribute<CollectionTag> Tags = new(Namespace, nameof(Tags));
    
    /// <summary>
    /// The number of endorsements the collection has.
    /// </summary>
    public static readonly ULongAttribute Endorsements = new(Namespace, nameof(Endorsements));

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
}
