using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

/// <summary>
/// A mod that was bundled with a collection
/// </summary>
[Include<NexusCollectionItemLoadoutGroup>]
public partial class NexusCollectionBundledLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.NexusCollectionBundledLoadoutGroup";

    /// <summary>
    /// The downloaded collection archive that this mod was bundled with
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsCollectionLibraryFile> CollectionLibraryFile = new(Namespace, nameof(CollectionLibraryFile));

    /// <summary>
    /// Reference to the original download.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionDownloadBundled> BundleDownload = new(Namespace, nameof(BundleDownload));
}
