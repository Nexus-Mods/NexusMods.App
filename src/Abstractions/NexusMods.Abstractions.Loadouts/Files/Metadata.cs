using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// Metadata are additonal entities attached to files that provide additional information about the file.
/// </summary>
[Obsolete(message: "This will be removed when moving to Loadout Items")]
public partial class Metadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Files.Metadata";
    
    /// <summary>
    /// The revision number of the MetaData
    /// </summary>
    public static readonly ULongAttribute Revision = new(Namespace, nameof(Revision));
    
    /// <summary>
    /// The file this metadata belongs to
    /// </summary>
    public static readonly ReferenceAttribute<File> File = new(Namespace, nameof(File));
}
