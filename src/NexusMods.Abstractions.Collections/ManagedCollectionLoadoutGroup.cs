using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

/// <summary>
/// Represents an uploaded collection managed by the app.
/// </summary>
[Include<CollectionGroup>]
public partial class ManagedCollectionLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.ManagedCollectionLoadoutGroup";

    /// <summary>
    /// The collection.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionMetadata> Collection = new(Namespace, nameof(Collection)) { IsIndexed = true };

    /// <summary>
    /// The revision number of the last published revision.
    /// </summary>
    public static readonly RevisionNumberAttribute LastPublishedRevisionNumber = new(Namespace, nameof(LastPublishedRevisionNumber)) { IsOptional = true };

    /// <summary>
    /// The current revision number.
    /// </summary>
    public static readonly RevisionNumberAttribute CurrentRevisionNumber = new(Namespace, nameof(CurrentRevisionNumber));
}
