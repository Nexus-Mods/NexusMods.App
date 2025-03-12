using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.UnrealEngine.Models;

[PublicAPI]
[Include<LoadoutItemGroup>]
public partial class UnrealEngineLogicLoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.UnrealEngine.UnrealEngineLogicLoadoutItem";
    
    /// <summary>
    /// Marker for logic mods - currently unused.
    /// </summary>
    public static readonly MarkerAttribute Marker = new(Namespace, nameof(Marker)) { IsOptional = true };
}
