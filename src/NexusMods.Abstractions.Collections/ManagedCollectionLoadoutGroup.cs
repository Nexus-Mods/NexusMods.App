using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
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

    /// <summary>
    /// The ID of the current revision. Only available if the
    /// current revision has been uploaded.
    /// </summary>
    public static readonly RevisionIdAttribute CurrentRevisionId = new(Namespace, nameof(CurrentRevisionId)) { IsOptional = true };

    /// <summary>
    /// Date when revision was last uploaded.
    /// </summary>
    public static readonly TimestampAttribute LastUploadDate = new(Namespace, nameof(LastUploadDate));

    /// <summary>
    /// Creates a <see cref="RevisionStatus"/>.
    /// </summary>
    public static RevisionStatus ToStatus(RevisionNumber currentRevisionNumber, Optional<RevisionNumber> lastPublishedRevision)
    {
        if (!lastPublishedRevision.HasValue) return RevisionStatus.Draft;
        return lastPublishedRevision.Value == currentRevisionNumber ? RevisionStatus.Published : RevisionStatus.Draft;
    }

    public partial struct ReadOnly
    {
        /// <inheritdoc cref="ManagedCollectionLoadoutGroup.ToStatus"/>
        public RevisionStatus ToStatus() => ManagedCollectionLoadoutGroup.ToStatus(CurrentRevisionNumber, LastPublishedRevisionNumber);
    }
}
