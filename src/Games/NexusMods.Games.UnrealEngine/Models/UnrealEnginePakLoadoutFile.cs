using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.UnrealEngine.Models;

[Include<LoadoutFile>]
public partial class UnrealEnginePakLoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.UnrealEngine.UnrealEnginePakLoadoutFile";

    /// <summary>
    /// Marker for pak file (if there is one).
    /// </summary>
    public static readonly MarkerAttribute PakFile = new(Namespace, nameof(PakFile));
    
    /// <summary>
    /// Reference to the archive that installed this pak file.
    /// </summary>
    public static readonly ReferenceAttribute<LibraryArchive> LibraryArchive = new(Namespace, nameof(LibraryArchive));
    
    /// <summary>
    /// Reference to the loadout item group that this file is part of.
    /// </summary>
    public static readonly ReferenceAttribute<LoadoutItemGroup> LoadoutItemGroup = new(Namespace, nameof(LoadoutItemGroup));
}
