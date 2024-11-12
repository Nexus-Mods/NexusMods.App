using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[Include<LoadoutItemGroup>]
public partial class ModLoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.MountAndBlade2Bannerlord.ModLoadoutItem";

    public static readonly ReferenceAttribute<ModuleInfoFileLoadoutFile> ModuleInfo = new(Namespace, nameof(ModuleInfo));
}
