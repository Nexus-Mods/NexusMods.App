using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[Include<LoadoutFile>]
public partial class ModuleInfoFileLoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.MountAndBlade2Bannerlord.ModuleInfoLoadoutFile";

    public static readonly MarkerAttribute ModuleInfoFile = new(Namespace, nameof(ModuleInfoFile));
}
