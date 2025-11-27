using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;

public partial class LoadoutItemGroupPriority : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutItemGroupPriority";

    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
    public static readonly ReferenceAttribute<LoadoutItemGroup> Target = new(Namespace, nameof(Target));
    public static readonly ConflictPriorityAttribute Priority = new(Namespace, nameof(Priority));
}
