using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// Represents an individual file which belongs to a <see cref="Loadout"/>, all files
/// should at least have the <see cref="Loadout"/> reference, and optionally a reference to a <see cref="Mod"/>,
/// </summary>
public partial class File : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Mods.ModFile";

    /// <summary>
    /// The loadout this file is part of.
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
    
    /// <summary>
    /// The mod this file belongs to, if any.
    /// </summary>
    public static readonly ReferenceAttribute<Mod> Mod = new(Namespace, nameof(Mod));
    
    /// <summary>
    /// The location the file will be installed to
    /// </summary>
    public static readonly GamePathAttribute To = new(Namespace, nameof(To));
    
    /// <summary>
    /// The file's metadata
    /// </summary>
    public static readonly BackReferenceAttribute<Metadata> Metadata = new(Files.Metadata.File);

}
