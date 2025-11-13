using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Abstractions.Loadouts;

public partial class DiskStateEntry : IModelDefinition
{
    /// <summary>
    /// Put entries in a user partition so they are all grouped together
    /// </summary>
    public static readonly PartitionId EntryPartition = PartitionId.User(4);
    
    private const string Namespace = "NexusMods.Loadouts.DiskStateEntry";
    
    /// <summary>
    /// The path to the file
    /// </summary>
    public static readonly GamePathParentAttribute Path = new(Namespace, nameof(Path));

    /// <summary>
    /// The hash of the file
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));
    
    /// <summary>
    /// The size of the file (in bytes)
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    
    /// <summary>
    /// The last modified time of the file
    /// </summary>
    public static readonly TimestampAttribute LastModified = new(Namespace, nameof(LastModified));

    /// <summary>
    /// The owning game installation
    /// </summary>
    public static readonly ReferenceAttribute<Sdk.Games.GameInstallMetadata> Game = new(Namespace, nameof(Game));
    
    
    public partial struct ReadOnly : IHavePathHashSizeAndReference
    {
#region IHavePathHashSizeAndReference

        GamePath IHavePathHashSizeAndReference.Path => DiskStateEntry.Path.Get(this);

        Hash IHavePathHashSizeAndReference.Hash => DiskStateEntry.Hash.Get(this);

        Size IHavePathHashSizeAndReference.Size => DiskStateEntry.Size.Get(this);
        EntityId IHavePathHashSizeAndReference.Reference => EntityId.From(0);
#endregion
    }
}
