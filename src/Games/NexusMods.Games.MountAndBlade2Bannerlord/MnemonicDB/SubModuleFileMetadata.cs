using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;


public partial class SubModuleFileMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models.SubModuleFileMetadata";

    /// <summary>
    /// Link to the sub-module info.
    /// </summary>
    public static readonly ReferenceAttribute<ModuleInfoExtended> ModuleInfo = new(Namespace, nameof(ModuleInfo));
    
    /// <summary>
    /// The NMA mod this sub-module file belongs to.
    /// </summary>
    public static readonly ReferenceAttribute<Mod> Mod = new(Namespace, nameof(Mod));
}
