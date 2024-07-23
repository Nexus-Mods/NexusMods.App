using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.DiskState.Models;

/// <summary>
/// Base attributes for the DiskState registry.
/// </summary>
public partial class DiskState : IModelDefinition
{
    private static readonly string Namespace = "NexusMods.DataModel.DiskStateRegistry";
    
    /// <summary>
    /// The associated game id.
    /// </summary>
    public static readonly GameDomainAttribute Game = new(Namespace, nameof(Game)) { IsIndexed = true, NoHistory = true };
    
    /// <summary>
    /// The game's root folder. Stored as a string, since AbsolutePaths require an IFileSystem, and we don't know or care
    /// what filesystem is being used when reading/writing the data from the database.
    /// </summary>
    public static readonly StringAttribute Root = new(Namespace, nameof(Root)) { IsIndexed = true, NoHistory = true };
    
    /// <summary>
    /// The entries in the disk state.
    /// </summary>
    public static readonly BackReferenceAttribute<DataModel.DiskState.Models.DiskStateEntry> Entries = new(DataModel.DiskState.Models.DiskStateEntry.DiskState);
}
