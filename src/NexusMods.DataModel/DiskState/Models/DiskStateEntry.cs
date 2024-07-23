using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.DiskState.Models;


/// <summary>
/// An entry in the disk state registry.
/// </summary>
public partial class DiskStateEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.DataModel.DiskState.DiskStateEntry";

    /// <summary>
    /// The disk state associated with this entry.
    /// </summary>
    public static readonly ReferenceAttribute<DiskState> DiskState = new(Namespace, nameof(DiskState));

    /// <summary>
    /// The path of the file
    /// </summary>
    public static readonly GamePathAttribute Path = new(Namespace, nameof(Path));
    
    /// <summary>
    /// The hash of the file
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));
    
    /// <summary>
    /// The last modified time of the file
    /// </summary>
    public static readonly DateTimeAttribute LastModified = new(Namespace, nameof(LastModified));
}

