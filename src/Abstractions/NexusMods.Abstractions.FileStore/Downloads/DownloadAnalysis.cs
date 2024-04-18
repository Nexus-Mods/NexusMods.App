using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using ModFileTreeNode = NexusMods.Paths.Trees.KeyedBox<NexusMods.Paths.RelativePath, NexusMods.Abstractions.FileStore.Trees.ModFileTree>;

namespace NexusMods.Abstractions.FileStore.Downloads;

/// <summary>
/// Attributes for the analysis data for a downloaded file
/// </summary>
public static class DownloadAnalysis
{
    private const string Namespace = "NexusMods.Abstractions.FileStore.Downloads.DownloadAnalysis";
    /// <summary>
    /// The hash of the downloaded archive from which this download is sourced from.
    /// </summary>
    /// <remarks>
    ///     If installed directly from folder (for dev purposes etc.), this may not exist
    /// </remarks>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) {IsIndexed = true};

    /// <summary>
    /// Size of the downloaded archive from which this download is sourced from.
    /// </summary>
    /// <remarks>
    ///     If installed directly from folder (for dev purposes etc.), this may not exist
    /// </remarks>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    
    
    /// <summary>
    /// Number of entries in the download
    /// </summary>
    public static readonly ULongAttribute NumberOfEntries = new(Namespace, nameof(NumberOfEntries));
    
    
    public class Model(ITransaction tx) : AEntity(tx)
    {
        
        /// <summary>
        /// The hash of the downloaded archive from which this download is sourced from.
        /// </summary>
        public Hash Hash
        {
            get => DownloadAnalysis.Hash.Get(this);
            set => DownloadAnalysis.Hash.Add(this, value);
        } 
        
        /// <summary>
        /// Size of the downloaded archive from which this download is sourced from.
        /// </summary>
        public Size Size
        {
            get => DownloadAnalysis.Size.Get(this);
            set => DownloadAnalysis.Size.Add(this, value);
        }

        /// <summary>
        /// The contents of the download
        /// </summary>
        public Entities<EntityIds, DownloadContentEntry.Model> Content
            => GetReverse<DownloadContentEntry.Model>(DownloadContentEntry.DownloadAnalysis);
    }
}


/// <summary>
/// A single entry in the download analysis, this is a file that is contained in the download
/// </summary>
public static class DownloadContentEntry
{
    private const string Namespace = "NexusMods.Abstractions.FileStore.Downloads.DownloadContentEntry";
    
    /// <summary>
    /// Hash of the file
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) {IsIndexed = true};

    /// <summary>
    /// Size of the file
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));

    /// <summary>
    /// Path of the file
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path));
    
    /// <summary>
    /// The DownloadAnalysis that this entry is a part of
    /// </summary>
    public static readonly ReferenceAttribute DownloadAnalysis = new(Namespace, nameof(DownloadAnalysis));
    
    public class Model(ITransaction tx) : AEntity(tx)
    {
        
        /// <summary>
        /// Hash of the file
        /// </summary>
        public Hash Hash
        {
            get => DownloadContentEntry.Hash.Get(this);
            set => DownloadContentEntry.Hash.Add(this, value);
        } 
        
        /// <summary>
        /// Size of the file
        /// </summary>
        public Size Size
        {
            get => DownloadContentEntry.Size.Get(this);
            set => DownloadContentEntry.Size.Add(this, value);
        }
        
        /// <summary>
        /// Path of the file
        /// </summary>
        public RelativePath Path
        {
            get => DownloadContentEntry.Path.Get(this);
            set => DownloadContentEntry.Path.Add(this, value);
        }
    }
    
}

/// <summary>
///     Creates the tree! From the download content entries.
/// </summary>
public class TreeCreator
{
    /// <summary>
    ///     Creates the tree! From the download content entries.
    /// </summary>
    /// <param name="downloads">Downloads from the download registry.</param>
    /// <param name="fs">FileStore to read the files from.</param>
    public static ModFileTreeNode Create(IReadOnlyCollection<DownloadContentEntry.Model> downloads, IFileStore? fs = null)
    {
        var entries = GC.AllocateUninitializedArray<ModFileTreeSource>(downloads.Count);
        var entryIndex = 0;
        foreach (var dl in downloads)
            entries[entryIndex++] = CreateSource(dl, fs != null ? new FileStoreStreamFactory(fs, dl.Hash) { Name = dl.Path, Size = dl.Size } : null);

        return ModFileTree.Create(entries);
    }

    /// <summary>
    ///     Creates a source file for the ModFileTree given a downloaded entry.
    /// </summary>
    /// <param name="entry">Entry for the individual file.</param>
    /// <param name="factory">Factory used to provide access to the file.</param>
    public static ModFileTreeSource CreateSource(DownloadContentEntry.Model entry, IStreamFactory? factory)
    {
        return new ModFileTreeSource()
        {
            Hash = entry.Hash.Value,
            Size = entry.Size.Value,
            Path = entry.Path,
            Factory = factory
        };
    }
}
