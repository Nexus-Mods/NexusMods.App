using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

[Include<ModuleFileMetadata>]
public partial class SubModuleFileMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models.SubModuleFileMetadata";

    /// <summary>
    /// Link to the sub-module info.
    /// </summary>
    public static readonly ReferenceAttribute<ModuleInfoExtended> ModuleInfo = new(Namespace, nameof(ModuleInfo));
}
