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
public static class ArchivedFile
{
    private const string Namespace = "NexusMods.DataModel.ArchivedFile";
    
    /// <summary>
    /// The compressed container (.nx archive) that contains the file, the entity referenced
    /// here should have the relative path to the file.
    /// </summary>
    public static ReferenceAttribute Container => new(Namespace, nameof(Container));
    
    /// <summary>
    /// The hash of the file entry
    /// </summary>
    public static HashAttribute Hash => new(Namespace, nameof(Hash)) {IsIndexed = true};
    
    /// <summary>
    /// The file entry data for the NX block offset data
    /// </summary>
    public static NxFileEntryAttribute NxFileEntry => new(Namespace, nameof(NxFileEntry));


    /// <summary>
    /// Model for the archived file entry.
    /// </summary>
    public class Model(ITransaction tx) : AEntity(tx)
    {
        
        /// <summary>
        /// Id of the containing archive.
        /// </summary>
        public EntityId ContainerId
        {
            get => ArchivedFile.Container.Get(this);
            set => ArchivedFile.Container.Add(this, value);
        } 
        
        /// <summary>
        /// The container that contains this file.
        /// </summary>
        public ArchivedFileContainer.Model Container
        {
            get => Db.Get<ArchivedFileContainer.Model>(ContainerId);
            set => ContainerId = value.Id;
        }
        
        /// <summary>
        /// Hash of the file entry
        /// </summary>
        public Hash Hash
        {
            get => ArchivedFile.Hash.Get(this);
            set => ArchivedFile.Hash.Add(this, value);
        }
        
        /// <summary>
        /// The Nx file entry data for the NX block offset data
        /// </summary>
        public FileEntry NxFileEntry
        {
            get => ArchivedFile.NxFileEntry.Get(this);
            set => ArchivedFile.NxFileEntry.Add(this, value);
        }
    }
    
}
