using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// Attributes for each entry in the HashCache
/// </summary>
public static class HashCacheEntry
{
    private const string Namespace = "NexusMods.Abstractions.DiskState.HashCacheEntry";
    
    /// <summary>
    /// The xxHash64 hash of the filename
    /// </summary>
    public static readonly HashAttribute NameHash = new(Namespace, nameof(NameHash)) { IsIndexed = true, NoHistory = true };
    
    /// <summary>
    /// The last time the file was modified
    /// </summary>
    public static readonly TimestampAttribute LastModified = new(Namespace, nameof(LastModified)) { NoHistory = true };
    
    /// <summary>
    /// The hash of the file
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { NoHistory = true };
    
    /// <summary>
    /// The size of the file
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size)) { NoHistory = true };


    public class Model(ITransaction tx) : Entity(tx, (byte)IdPartitions.HashCache)
    {
        /// <summary>
        /// The xxHash64 of the name
        /// </summary>
        public Hash NameHash
        {
            get => HashCacheEntry.NameHash.Get(this);
            set => HashCacheEntry.NameHash.Add(this, value);
        }
        
        /// <summary>
        /// The xxHash64 hash of the file
        /// </summary>
        public Hash Hash
        {
            get => HashCacheEntry.Hash.Get(this);
            set => HashCacheEntry.Hash.Add(this, value);
        }
        
        /// <summary>
        /// Last time the file was modified
        /// </summary>
        public DateTime LastModified
        {
            get => HashCacheEntry.LastModified.Get(this);
            set => HashCacheEntry.LastModified.Add(this, value);
        }
        
        /// <summary>
        /// The size of the file
        /// </summary>
        public Size Size
        {
            get => HashCacheEntry.Size.Get(this);
            set => HashCacheEntry.Size.Add(this, value);
        }
    }
    
}
