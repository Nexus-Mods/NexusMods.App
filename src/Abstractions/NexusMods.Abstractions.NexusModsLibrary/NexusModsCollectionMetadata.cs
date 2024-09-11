using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Metadata about a collection on Nexus Mods.
/// </summary>
public partial class NexusModsCollectionMetadata : IModelDefinition
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
    /// The revisions of the collection.
    /// </summary>
    public static readonly BackReferenceAttribute<NexusModsCollectionRevision> Revisions = new(NexusModsCollectionRevision.Collection);
}
