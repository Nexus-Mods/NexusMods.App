using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

public partial class CollectionRevisionModFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.CollectionRevisionModFile";
    
    /// <summary>
    /// The Nexus, globally unique id identifying a specific file of a collection revision.
    /// </summary>
    public static readonly ULongAttribute FileId = new(Namespace, nameof(FileId)) { IsIndexed = true };
    
    /// <summary>
    /// If the file is optional or not
    /// </summary>
    public static readonly BooleanAttribute IsOptional = new(Namespace, nameof(IsOptional));
    
    /// <summary>
    /// The associated NexusModsFileMetadata that contains the other metadata of the file.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsFileMetadata> NexusModFile = new(Namespace, nameof(NexusModFile));
    
    /// <summary>
    /// The associated CollectionRevision 
    /// </summary>
    public static readonly ReferenceAttribute<CollectionRevisionMetadata> CollectionRevision = new(Namespace, nameof(CollectionRevision));
}
