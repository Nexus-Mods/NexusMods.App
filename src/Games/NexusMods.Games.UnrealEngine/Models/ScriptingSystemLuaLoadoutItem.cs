using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Abstractions.Loadouts.Extensions;

namespace NexusMods.Games.UnrealEngine.Models;

[PublicAPI]
[Include<LoadoutItemGroup>]
public partial class ScriptingSystemLuaLoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.UnrealEngine.ScriptingSystemLuaLoadoutItem";

    /// <summary>
    /// Marker for lua mods
    /// </summary>
    public static readonly MarkerAttribute Marker = new(Namespace, nameof(Marker)) { IsOptional = true };
}
