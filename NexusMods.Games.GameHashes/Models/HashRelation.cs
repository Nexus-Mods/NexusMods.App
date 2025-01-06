using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Games.GameHashes.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.GameHashes.Models;

public partial class HashRelation : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.GameHashes.HashRelation";

    /// <summary>
    /// The primary xxHash3
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, "Hash") { IsIndexed = true };

    /// <summary>
    /// The size of the file
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, "Size") { IsIndexed = true };

    /// <summary>
    /// The minimal hash
    /// </summary>
    public static readonly HashAttribute MinimalistHash = new(Namespace, "MinimalistHash") { IsIndexed = true };
    
    /// <summary>
    /// The Sha1 hash attribute
    /// </summary>
    public static readonly Sha1Attribute Sha1 = new(Namespace, "Sha1") { IsIndexed = true };
}
