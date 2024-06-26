using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public partial class SubModuleInfo : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models";
    
    /// <summary>
    /// Name of the sub-module.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// The name of the DLL.
    /// </summary>
    public static readonly StringAttribute DLLName = new(Namespace, nameof(DLLName));
    
    /// <summary>
    /// Assembly names of the sub-module.
    /// </summary>
    public static readonly StringAttribute Assemblies = new(Namespace, nameof(Assemblies)) { Cardinalty = Cardinality.Many };

}
