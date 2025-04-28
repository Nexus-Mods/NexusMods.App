using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

[Include<LoadoutItemGroup>]
public partial class NexusCollectionItemLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.NexusCollectionItemLoadoutGroup";

    /// <summary>
    /// Whether the item is required or optional.
    /// </summary>
    public static readonly BooleanAttribute IsRequired = new(Namespace, nameof(IsRequired)) { IsIndexed = true };

    /// <summary>
    /// Reference to the original download.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionDownload> Download = new(Namespace, nameof(Download));

    public partial struct ReadOnly
    {
        /// <summary>
        /// Whether the item is optional or required.
        /// </summary>
        public bool IsOptional => !IsRequired;
    }
}
