using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

/// <summary>
/// A collection loadout group that was sourced from a NexusMods collection library file.
/// </summary>
[Include<CollectionGroup>]
public partial class NexusCollectionLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.NexusCollectionLoadoutGroup";

    /// <summary>
    /// The collection.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionMetadata> Collection = new(Namespace, nameof(Collection)) { IsIndexed = true };

    /// <summary>
    /// The revision.
    /// </summary>
    public static readonly ReferenceAttribute<CollectionRevisionMetadata> Revision = new(Namespace, nameof(Revision)) { IsIndexed = true };

    /// <summary>
    /// The collection library file.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsCollectionLibraryFile> LibraryFile = new(Namespace, nameof(LibraryFile));

    public partial struct ReadOnly
    {
        public IEnumerable<NexusCollectionItemLoadoutGroup.ReadOnly> GetCollectionItems()
        {
            return Db
                .GetBackRefs(LoadoutItem.Parent, Id)
                .AsModels<NexusCollectionItemLoadoutGroup.ReadOnly>(Db)
                .Where(static model => model.IsValid());
        }
    }
}
