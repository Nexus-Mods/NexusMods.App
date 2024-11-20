using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Represents a download inside a collection.
/// </summary>
[PublicAPI]
public partial class CollectionDownload : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionDownload";

    /// <summary>
    /// The revision this download is a part of.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionRevisionMetadata> CollectionRevision = new(Namespace, nameof(CollectionRevision));

    /// <summary>
    /// Name of item in the Loadout.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// Whether the download is optional.
    /// </summary>
    public static readonly BooleanAttribute IsOptional = new(Namespace, nameof(IsOptional));
}
