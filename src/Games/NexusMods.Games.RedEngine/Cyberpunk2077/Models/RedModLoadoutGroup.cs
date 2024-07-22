using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

[Include<LoadoutItemGroup>]
public partial class RedModLoadoutGroup : IModelDefinition
{
    public const string Namespace = "NexusMods.Games.RedEngine.Cyberpunk2077.RedModLoadoutGroup";

    /// <summary>
    /// The internal redmod name of this mod, this may differ from what is shown in the UI;
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The internal version number of the mod
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));
    
    /// <summary>
    /// Optional description of the mod
    /// </summary>
    public static readonly StringAttribute Description = new(Namespace, nameof(Description)) { IsOptional = true };


}
