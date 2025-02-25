using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.UnrealEngine.Models;

[Include<LoadoutItemGroup>]
public partial class ScriptingSystemLoadoutItemGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.UnrealEngine.ScriptingSystemLoadoutItemGroup";

    // Marks the UE4SS LoadoutItemGroup
    public static readonly MarkerAttribute Marker = new(Namespace, nameof(Marker));
}
