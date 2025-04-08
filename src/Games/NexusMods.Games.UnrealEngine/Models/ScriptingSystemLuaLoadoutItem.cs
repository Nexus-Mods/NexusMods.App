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
    /// The Lua mod's parent folder in the game's content folder. It's used to tell UE4SS the
    ///  order in which to load these mods.
    /// </summary>
    public static readonly StringAttribute LoadOrderName = new(Namespace, nameof(LoadOrderName)) { IsOptional = false };
}
