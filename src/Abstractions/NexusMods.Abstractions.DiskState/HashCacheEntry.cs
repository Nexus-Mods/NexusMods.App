using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// Attributes for each entry in the HashCache
/// </summary>
public partial class HashCacheEntry : IModelDefinition
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
}
