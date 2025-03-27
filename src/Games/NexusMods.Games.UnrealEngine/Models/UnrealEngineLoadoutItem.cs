using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.UnrealEngine.Models;

[PublicAPI]
[Include<LoadoutItemGroup>]
public partial class UnrealEngineLoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.UnrealEngine.UnrealEngineLoadoutItem";
    
    /// <summary>
    /// The prefix of the loadout item - this is used for load order.
    /// </summary>
    public static readonly StringAttribute LOPrefix = new(Namespace, nameof(LOPrefix)) { IsOptional = true };
}
