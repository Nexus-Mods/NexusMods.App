using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.Models;

[PublicAPI]
[Include<LibraryArchive>]
public partial class ManuallyCreatedArchive : IModelDefinition
{
    public enum CreationSource
    {
        /// <summary>
        /// This mod was created from files in a downloaded collection. This normally means the user
        /// cloned the collection and so bundled mods were removed and added to this mod.
        /// </summary>
        CollectionBundled = 1,
    }
    
    private const string Namespace = "NexusMods.Library.ManuallyCreatedArchive";

    /// <summary>
    /// The source of this archive
    /// </summary>
    public static readonly EnumAttribute<CreationSource> Source = new(Namespace, nameof(CreationSource));
}
