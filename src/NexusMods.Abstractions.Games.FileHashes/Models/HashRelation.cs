using NexusMods.Abstractions.Games.FileHashes.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using Md5Attribute = NexusMods.Abstractions.Games.FileHashes.Attributes.Md5Attribute;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

/// <summary>
/// A relation of hashes. Given one hash, you can find the others.
/// </summary>
public partial class HashRelation : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.HashRelation";

    /// <summary>
    /// The XxHash3 hash of the file
    /// </summary>
    public static readonly HashAttribute XxHash3 = new(Namespace, nameof(XxHash3)) { IsIndexed = true };
    
    /// <summary>
    /// The XxHash64 hash of the file stored as a ulong (normal hash type)
    /// </summary>
    public static readonly HashAttribute XxHash64 = new(Namespace, nameof(XxHash64)) { IsIndexed = true };
    
    /// <summary>
    /// The MinimalHash hash of the file
    /// </summary>
    public static readonly HashAttribute MinimalHash = new(Namespace, nameof(MinimalHash)) { IsIndexed = true };
    
    /// <summary>
    /// The MD5 hash of the file
    /// </summary>
    public static readonly Md5Attribute Md5 = new(Namespace, nameof(Md5)) { IsIndexed = true };
    
    /// <summary>
    /// The SHA1 hash of the file
    /// </summary>
    public static readonly Sha1Attribute Sha1 = new(Namespace, nameof(Sha1)) { IsIndexed = true };
    
    /// <summary>
    /// The CRC32 hash of the file
    /// </summary>
    public static readonly Crc32Attribute Crc32 = new(Namespace, nameof(Crc32)) { IsIndexed = true };
    
    /// <summary>
    /// The size of the file in bytes
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size)) { IsIndexed = true };

    /// <summary>
    /// The paths that are associated with this hash relation.
    /// </summary>
    public static readonly BackReferenceAttribute<PathHashRelation> Paths = new(PathHashRelation.Hash);

}
