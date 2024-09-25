using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
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
    /// The collections' image.
    /// </summary>
    public static readonly MemoryAttribute TileImage = new(Namespace, nameof(TileImage));
    
    /// <summary>
    /// The collections' image.
    /// </summary>
    public static readonly MemoryAttribute BackgroundImage = new(Namespace, nameof(BackgroundImage));
}
