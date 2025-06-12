using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

[Include<CollectionGroup>]
public partial class ManagedCollectionLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.ManagedCollectionLoadoutGroup";

    /// <summary>
    /// The collection.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionMetadata> Collection = new(Namespace, nameof(Collection)) { IsIndexed = true };

    /// <summary>
    /// The revision.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionRevisionMetadata> LastUploadedRevision = new(Namespace, nameof(LastUploadedRevision)) { IsIndexed = true };
}
