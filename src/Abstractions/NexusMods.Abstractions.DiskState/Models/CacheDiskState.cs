using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.Abstractions.DiskState.Models;

[Include<DiskState>]
public class CacheDiskEntry : IModelDefinition
{
    public const string Namespace = "NexusMods.Abstractions.DiskState.Models";

    /// <summary>
    /// The path to the file
    /// </summary>
    public static readonly StringAttribute Path = new(Namespace, nameof(Path));
    

    public static readonly Dat
    
}
