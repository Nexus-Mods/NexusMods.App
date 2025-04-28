using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

/// <summary>
/// A relation between a file path and a hash, one or more game builds may reference the same path and hash relation.
/// </summary>
public partial class PathHashRelation : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.PathHashRelation";
    
    /// <summary>
    /// The path of the file
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path)) { IsIndexed = true };
    
    /// <summary>
    /// The hash of the file
    /// </summary>
    public static readonly ReferenceAttribute<HashRelation> Hash = new(Namespace, nameof(Hash));
}
