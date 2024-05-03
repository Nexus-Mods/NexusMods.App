using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

public static class ModuleInfoExtended
{
    private const string Namespace = "NexusMods.Games.MountAndBlade2Bannerlord.Models";
    
    /// <summary>
    /// The module ID.
    /// </summary>
    public static readonly StringAttribute ModuleId = new(Namespace, nameof(ModuleId)) {IsIndexed = true};
    
    /// <summary>
    /// The module name.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// True if the module is official.
    /// </summary>
    public static readonly BooleanAttribute IsOfficial = new(Namespace, nameof(IsOfficial));
    
    /// <summary>
    /// Application version of the module.
    /// </summary>
    public static readonly ApplicationVersionAttribute Version = new(Namespace, nameof(Version));
    
    /// <summary>
    /// True if the module is a singleplayer module.
    /// </summary>
    public static readonly BooleanAttribute IsSingleplayerModule = new(Namespace, nameof(IsSingleplayerModule));
    
    /// <summary>
    /// True if the module is a multiplayer module.
    /// </summary>
    public static readonly BooleanAttribute IsMultiplayerModule = new(Namespace, nameof(IsMultiplayerModule));
    
    /// <summary>
    /// True if this is a server module.
    /// </summary>
    public static readonly BooleanAttribute IsServerModule = new(Namespace, nameof(IsServerModule));
    
}
