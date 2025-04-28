using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
namespace NexusMods.Abstractions.FileStore.Nx.Models;

/// <summary>
/// Represents a container for archived files. 
/// </summary>
public partial class ArchivedFileContainer : IModelDefinition
{
    private const string Namespace = "NexusMods.ArchiveContents.ArchivedFileContainer";
    
    /// <summary>
    /// The name of the container on-disk. This will be relative to some archive root path.
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path));
}
