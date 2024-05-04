using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public static class SubModuleInfo
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
    public static readonly StringsAttribute Assemblies = new(Namespace, nameof(Assemblies));
    
    public class Model(ITransaction tx) : Entity(tx)
    {
        public string Name
        {
            get => SubModuleInfo.Name.Get(this);
            init => SubModuleInfo.Name.Add(this, value);
        }

        public string DLLName
        {
            get => SubModuleInfo.DLLName.Get(this);
            init => SubModuleInfo.DLLName.Add(this, value);
        }
        
        public IEnumerable<string> Assemblies => SubModuleInfo.Assemblies.Get(this);
    }
    
}
