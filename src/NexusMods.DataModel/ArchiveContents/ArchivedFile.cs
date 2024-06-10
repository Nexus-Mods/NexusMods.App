using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.DataModel.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
// ReSharper disable MemberHidesStaticFromOuterClass

namespace NexusMods.DataModel.ArchiveContents;

/// <summary>
/// A metadata entry for an archived file entry. These are the items stored inside the .nx archives. Each
/// entry contains the hash, the decompressed size, and a reference to the container. In the case of Nx containers
/// it also contains the Nx internal header data for the entry so that we can do point lookups, of files.
/// </summary>
public partial class ArchivedFile : IModelDefinition
{
    private const string Namespace = "NexusMods.DataModel.ArchivedFile";
    
    /// <summary>
    /// The compressed container (.nx archive) that contains the file, the entity referenced
    /// here should have the relative path to the file.
    /// </summary>
    public static readonly ReferenceAttribute<ArchivedFileContainer> Container = new(Namespace, nameof(Container));
    
    /// <summary>
    /// The hash of the file entry
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) {IsIndexed = true};
    
    /// <summary>
    /// The file entry data for the NX block offset data
    /// </summary>
    public static readonly NxFileEntryAttribute NxFileEntry = new(Namespace, nameof(NxFileEntry));
}
